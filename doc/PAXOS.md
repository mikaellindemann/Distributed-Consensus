# Why Paxos cannot be used

## What it can do


## What it cannot do
- It assumes that only non-byzantine errors occurs.
- It assumes that messages do not get tampered with - maybe this is not important since it can be solved my encryption/signing of messages.
- It assumes that all nodes should know all information.
- It assumes that all nodes know eachother or atleast the majority.