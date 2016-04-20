using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Common.DTO.Event;
using Common.DTO.Shared;
using Newtonsoft.Json;

namespace Client.Services
{
    public class DcrParser
    {
        private readonly Dictionary<string, EventDto> _map;
        private readonly XDocument _xDoc;
        private readonly string _workflowId;
        private readonly string[] _ips;
        private readonly HashSet<string> _rolesSet;

        public Dictionary<string, string> IdToAddress { get; set; }

        public static DcrParser Parse(string filePath, string workflowId, string[] eventIps)
        {
            var parser = new DcrParser(filePath, workflowId, eventIps);
            //ORDER OF METHOD CALL IS IMPORTANT, MUST be THIS!
            parser.InitiateAllEventAddressDtoWithRolesAndNames();
            parser.MapDcrIdToRealId();
            parser.DelegateIps();
            parser.Constraints();
            parser.States();

            return parser;
        }

        private DcrParser(string filePath, string workflowId, string[] eventIps)
        {
            IdToAddress = new Dictionary<string, string>();
            _rolesSet = new HashSet<string>();
            _ips = eventIps;
            _workflowId = workflowId;
            _map = new Dictionary<string, EventDto>();
            _xDoc = XDocument.Load(filePath);
        }

        private void InitiateAllEventAddressDtoWithRolesAndNames()
        {
            var events = _xDoc.Descendants("events").Descendants("event");
            foreach (var element in events)
            {
                EventDto eventDto;

                var dto = ExtractFromId(element, out eventDto);

                //Get roles.
                var role = element.Descendants("roles").Descendants("role");
                foreach (var r in role.Select(r => r.Value))
                {
                    _rolesSet.Add(r);
                    ((HashSet<string>)eventDto.Roles).Add(r);
                }

                //Get name & description.
                var desc = element.Descendants("eventDescription");
                foreach (var d in desc.Select(d => d.Value))
                {
                    eventDto.Name = d;
                }

                //Save to map.
                _map[dto] = eventDto;
            }
        }

        private string ExtractFromId(XElement element, out EventDto eventDto)
        {
            var dto = element.Attribute("id").Value;
            var exists = _map.ContainsKey(dto);

            if (exists) {
                _map.TryGetValue(dto, out eventDto);
            }
            else {
                eventDto = new EventDto
                {
                    Responses = new HashSet<EventAddressDto>(),
                    Conditions = new HashSet<EventAddressDto>(),
                    Roles = new HashSet<string>(),
                    Inclusions = new HashSet<EventAddressDto>(),
                    Exclusions = new HashSet<EventAddressDto>(),
                    Executed = false,
                    Included = false,
                    Pending = false,
                    WorkflowId = _workflowId,
                };
            }
            return dto;
        }

        private void MapDcrIdToRealId()
        {
            var eventIds = _xDoc.Descendants("labelMappings").Descendants("labelMapping");

            foreach (var element in eventIds)
            {
                var id = element.Attribute("eventId").Value;
                var eventId = element.Attribute("labelId").Value;
                var eventDto = _map[id];
                eventDto.EventId = PrettifyId(eventId);
                if (string.IsNullOrEmpty(eventDto.Name)) eventDto.Name = eventId;
                _map[id] = eventDto;
            }
        }

        private static string PrettifyId(string input)
        {
            return input.Trim().Replace(" ", "");
            // The statement below turns ReferToUBS into Refertoubs
            //var textInfo = new CultureInfo("en-US", false).TextInfo;
            //return textInfo.ToTitleCase((input.Replace(" ", "")));
        }

        private void DelegateIps()
        {
            var random = new Random();

            foreach (var v in _map.Values)
            {
                IdToAddress.Add(v.EventId, _ips[random.Next(_ips.Length)]);
            }
        }

        private void Constraints()
        {
            //Constraints general tag into variable
            var constraints = _xDoc.Descendants("constraints").ToList();

            ExtractRules(constraints, "conditions", "condition", eventDto => (ICollection<EventAddressDto>) eventDto.Conditions);
            ExtractRules(constraints, "responses", "response", eventDto => (ICollection<EventAddressDto>) eventDto.Responses);
            ExtractRules(constraints, "excludes", "exclude", eventDto => (ICollection<EventAddressDto>) eventDto.Exclusions);
            ExtractRules(constraints, "includes", "include", eventDto => (ICollection<EventAddressDto>) eventDto.Inclusions);
        }

        private void ExtractRules(IEnumerable<XElement> constraints, string descendantParent, string descendant, Func<EventDto, ICollection<EventAddressDto>> getPropertyFunc)
        {
            var rules = constraints.Descendants(descendantParent).Descendants(descendant);

            foreach (var rule in rules)
            {
                string source;
                string target;

                //Anoying check to swap variables if we're dealing with conditions.
                if (descendant == "condition")
                {
                    target = rule.Attribute("sourceId").Value;
                    source = rule.Attribute("targetId").Value;
                }
                else
                {
                    source = rule.Attribute("sourceId").Value;
                    target = rule.Attribute("targetId").Value;
                }

                //If no Uri is provided, use a fake one.
                var uriString = IdToAddress[_map[target].EventId] ?? "http://ImNotReallyAProperURI.com";

                var toAdd = new EventAddressDto
                {
                    WorkflowId = _map[target].WorkflowId,
                    Id = _map[target].EventId,
                    Roles = _map[target].Roles,
                    Uri = new Uri(uriString)
                };

                var eventDto = _map[source];
                var ruleCollection = getPropertyFunc(eventDto);
                ruleCollection.Add(toAdd);

                _map[source] = eventDto;
            }
        }

        private void States()
        {
            //State stuff
            var state = _xDoc.Descendants("marking").ToList();

            //Executed
            ExtractStates(state, "executed", "event", eventDto => eventDto.Executed = true);

            //Included
            ExtractStates(state, "included", "event", eventDto => eventDto.Included = true);

            //Pending
            ExtractStates(state, "pendingResponses", "event", eventDto => eventDto.Pending = true);
        }

        private void ExtractStates(IEnumerable<XElement> state, string descendantParent, string descendant, Func<EventDto, bool> setPropertyFunc)
        {
            var executed = state.Descendants(descendantParent).Descendants(descendant);
            foreach (var e in executed)
            {
                var eventId = e.Attribute("id").Value;
                var eventDto = _map[eventId];
                setPropertyFunc(eventDto);
                _map[eventId] = eventDto;
            }
        }

        public async Task CreateJsonFile()
        {
            using (var sw = new StreamWriter(@"graph.json", false))
            {
                foreach (var v in _map.Values)
                {
                    var json = JsonConvert.SerializeObject(v, Formatting.Indented);
                    await sw.WriteLineAsync(json);
                    await sw.WriteLineAsync("");
                    await sw.WriteLineAsync("");
                }
            }
        }

        public IEnumerable<string> GetRoles()
        {
            return _rolesSet;
        }

        public Dictionary<string, EventDto> GetMap()
        {
            return _map;
        }
    }
}
