namespace HistoryConsensus

open Action

module DCRSimulator =
    type EventState = bool * bool * bool
    type DCRState = Map<EventId, EventState>

