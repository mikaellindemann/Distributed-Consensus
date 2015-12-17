# Bachelor - Distributed Consensus
The repository for our bachelor project concerning creating a valid history of events in a distributed system.

## Projektbeskrivelse

Vi vil gerne undersøge hvordan det er muligt at opbygge en historik over begivenheder i en DCR-graf.

## Initial notes
1. Klient spørger alle / Node har alle events i workflowet.
2. Knude spørger selv naboer i workflow (delvist lokal historik)
3. Knuder spørger rekursivt andre naboer (distribueret global historik)

Hvilke problemer har  vi hvis vi gerne vil løse denne historik distribueret (i netværket)?

Klient, håndter fejl -> Hvordan udnytter vi at vi modtager flere (forskellige) historikker til at håndtere korrupte knuder?
