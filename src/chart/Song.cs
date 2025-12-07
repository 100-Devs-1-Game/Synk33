using Godot;
using Godot.Collections;

namespace SYNK33.chart;

[GlobalClass]
public partial class Song : Resource {
	[Export] public string Name { get; set; }
	[Export] public string Author { get; set; }
	[Export] public string Genre { get; set; }
	[Export] public AudioStream Audio { get; set; }
	[Export] public Texture2D Cover { get; set; }
	[Export] public float Bpm { get; set; }
	[Export(PropertyHint.Flags, "Easy,Normal,Hard,Expert")] public int Difficulties { get; set; }
	private static Dictionary<Difficulty, string> DifficultyMap = new Dictionary<Difficulty, string> {
		{ Difficulty.Easy, "easy" },
		{ Difficulty.Normal, "normal" },
		{ Difficulty.Hard, "hard" },
		{ Difficulty.Expert, "expert" }
	};

	public Chart? GetChartByDifficulty(Difficulty difficulty) {
		if (!HasChart(difficulty)) {
			return null;
		}

		string path = $"{ResourcePath.GetBaseName()}_{DifficultyMap[difficulty]}.tres";
		return GD.Load<Chart>(path);
	}

	public bool HasChart(Difficulty difficulty) {
		return (Difficulties & (1 << (int)difficulty)) != 0;
	}
}

public enum Difficulty {
	Easy,
	Normal,
	Hard,
	Expert
}
