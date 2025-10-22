using Godot;
using Godot.Collections;

namespace SYNK33.chart;

[GlobalClass]
public partial class Song : Resource {
	[Export] public string Name { get; set; }
	[Export] public string Author { get; set; }
	[Export] public AudioStream Audio { get; set; }
	[Export] public Texture2D Cover { get; set; }
	[Export] public Array<Chart> Charts { get; set; }
}
