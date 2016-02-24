﻿using System.Globalization;

namespace Common.DTO.History
{
    public class ActionDto
    {
        public int TimeStamp { get; set; }
        public string EventId { get; set; }
        public string WorkflowId { get; set; }
        public string CounterPartId { get; set; }

        public ActionDto()
        {
            
        }
        public ActionDto(ActionModel model)
        {
            TimeStamp = model.Id;
            EventId = model.EventId;
            WorkflowId = model.WorkflowId;
            CounterPartId = model.CounterPartId;
        }
    }
}