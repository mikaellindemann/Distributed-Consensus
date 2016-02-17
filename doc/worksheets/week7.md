# Week 7
- Working on definitions of history, action and so on
- Examples of workflows and their results

## Things to remember for the implementation
- 

## Things which still needs to be decided
- Maybe we need to add signatures to messages such that malicious events do not change data of other events.
    - This needs to be done on all previous nodes which have returned history.
    - When history is returned from neighbours, check their signatures and all signatures of their recoursively called neighbours.
    - This is very heavy messagewise, but could imrpove security.