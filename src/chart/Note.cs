namespace SYNK33.chart;

public enum NoteType {
    Left,
    Middle,
    Right
}

public abstract record Note(float Beat, NoteType Type) {
    public sealed record Tap(float Beat, NoteType Type) : Note(Beat, Type);

    public sealed record Hold(float Beat, float EndBeat, NoteType Type) : Note(Beat, Type);
}