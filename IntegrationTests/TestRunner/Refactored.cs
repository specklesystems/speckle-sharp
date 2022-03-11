
[Flags]
public enum SpeckleTransportOperations 
{
    None = 0,
    Send = 1,
    Receive = 2
}

public interface PipelineContext {
  // whatever this is, that is useful to pass between operations, and be dumped in front of a human in case of a failure
}

// test workflow
// 1. get test configuration -> IAutotestConfig: app support matrix
// 2. initialize available apps based on config -> usable application handler registry
// 3. build executable test pipelines from config and available apps
// 4. execute test pipelines:
//   a. send a context object through the test pipeline to trace the steps of the workflow
//      ie: count the objects 