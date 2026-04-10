namespace InSpectra.Gen.Acquisition.App.Machine;


internal sealed record MachineEnvelope<T>(
    bool Ok,
    T? Data,
    MachineError? Error,
    MachineMeta Meta);
