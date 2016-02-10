# Week 6
- Created LaTeX-documents with initial structure and bibliography.
- Rename fetch to produce, and include stitch as part of the new produce-algorithm.
- Removed history-ID because we only see it as a performance optimization for now.
- We have had a hard time putting words on the algorithms, without describing every step of them, and in general a hard time describing the problems that needs to be solved, rather than how we plan to do it.
- Pseudo-code has been added for most algorithms. Some way more detailed than others.
  - Some pseudo-code just assumes the reader knows what happens. For instance when adding a node/edge to a graph, which relies heavily on the datastructure used.

## Things to remember for the implementation
- Every history entry (log) must include the event ID of the original event as well as the complete representation of the local timestamp for that event. - This makes it possible to validate the resulting graph when it is sent to the election.

## Things which still needs to be decided
- Data structures still needs to be defined, with relation to the needed operations.
