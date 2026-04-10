namespace InSpectra.Gen.Acquisition.App.Machine;


internal sealed record MachineError(
    string Kind,
    string Message);
