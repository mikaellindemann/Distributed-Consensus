# Bachelor Thesis - Consensus of History in Distributed DCR Graphs
The repository for our bachelor project concerning creating a valid history of events in a distributed system.

## Project Description

Dynamic Condition Response Graphs (“DCR Graphs”) are developed principally by Thomas Hildebrandt at the IT University of Copenhagen, in collaboration with ResultMaker and later Exformatics A/S. They are used to represent workflows, where nodes represents an activity, and edges between these nodes represent one of four different relations: condition, response, inclusion and exclusion. 

In a distributed DCR graph finding a history or log of a given order of execution can be difficult, due to the fact that no single node has the overview of the entire workflow. Logs can be split among several nodes, timestamps of logs do not necessarily correspond among nodes and nodes can emit erroneous logs. The nodes have to reach consensus on the history given these challenges. 

Given a distributed implementation of DCR Graphs, we would like to develop an algorithm that can exploit the rules of DCR Graphs, in order to put together which events have been executed and in what order. We will explore theoretical limits and practical implementation(s). 

By researching and applying distributed system theories and algorithms, as well as the rules of DCR Graphs, we will try to solve the problem of generating the history. 

The result will be evaluated comparing the generated history of the implementation and the actual history, while taking into account the amount of erroneous logs in the system. 

An implementation and description of the found solution, including a description of the problem domain, the solution to the problem and the final implementation, will be the final product of the project. 

A successful solution to the problem will include developing and analysing algorithms where: 
- a central node produces history locally
- neighboring nodes cooperate to produce history locally
- nodes in the graph cooperate to produce history globally
Each step should include an analysis of performance, an analysis of the impact of having malicious nodes in the graph, and an analysis of whether knowledge of the structure of the graph helps produce history. 

If time permits, the theoretical bounds of the possibility of producing history of a given graph can be explored.


## Prerequisites
Knowledge of object oriented and functional programming paradigms as well as knowledge in the area of distributed systems, algorithms and data structures. 

Furthermore, an understanding of DCR Graphs and their implementation as distributed systems. 

These prerequisites have been fulfilled by taking the following courses: 
- Algorithms and Data Structures
- Mobile and Distributed Systems
- Analysis, Design and Software Architecture
- Second Year Project: Functional Programming
- Second Year Project: Software Development in Large Teams with International Collaboration (Spring 2015 focused on DCR Graphs) 

## Initial notes
1. Klient spørger alle / Node har alle events i workflowet.
2. Knude spørger selv naboer i workflow (delvist lokal historik)
3. Knuder spørger rekursivt andre naboer (distribueret global historik)

Hvilke problemer har  vi hvis vi gerne vil løse denne historik distribueret (i netværket)?

Klient, håndter fejl -> Hvordan udnytter vi at vi modtager flere (forskellige) historikker til at håndtere korrupte knuder?
