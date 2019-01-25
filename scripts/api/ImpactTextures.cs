using UnityEngine;
using System.Collections.Generic;
using FileManagement;

public class ImpactTextures
{
	private Texture2D he_hole;
	private Texture2D ap_hole;
	private Texture2D armor_default;

	private bool he_found;
	private bool ap_found;
	private bool armor_found;

	private string filepath;

	public ImpactTextures(string p_filepath) {
		filepath = p_filepath;
		he_hole = new Texture2D(10, 10, TextureFormat.Alpha8, false);
		ap_hole = new Texture2D(10, 10, TextureFormat.Alpha8, false);
		armor_default = new Texture2D(1024, 1024, TextureFormat.Alpha8, false);
		LoadAll();

		if (!he_found) FileReader.FileLog("No he impact texture found", FileLogType.loader);
		if (!ap_found) FileReader.FileLog("No ap impact texture found", FileLogType.loader);
		if (!armor_found) FileReader.FileLog("No default armor texture found", FileLogType.loader);
	}

	public void LoadAll() {
		DataStructure texture_structure = DataStructure.Load(filepath);

		foreach (DataStructure child in texture_structure.AllChildren) {
			Texture2D texture = child.Get<Texture2D>("image");

			switch (child.Name) {
			case "ApHole":
				ap_hole = texture;
				ap_found = true;
				break;
			case "HeHole":
				he_hole = texture;
				he_found = true;
				break;
			case "ArmorDefault":
				armor_default = texture;
				armor_found = true;
				break;
			default:
				break;
			}
		}
	}

	public Texture2D GetTexture (TextureTemplate template, int width=0, int height=0) {
		switch (template) {
		case TextureTemplate.ap_hole:
			return ScaleTexture(ap_hole, width, height);
		case TextureTemplate.he_hole:
			return ScaleTexture(he_hole, width, height);
		default:
		case TextureTemplate.default_armor:
			return ScaleTexture(armor_default, width, height);
		}
	}

	public static Texture2D ScaleTexture (Texture2D source, int width=0, int height=0) {
		int act_width = width == 0 ? source.width : width;
		int act_height = height == 0 ? source.height : height;

		Texture2D res = new Texture2D(act_width, act_height, TextureFormat.Alpha8, false);

		Color[] pixels = new Color[act_width * act_height];
		int color_indexer = 0;
		for (int x=0; x < act_width; x++) {
			for (int y=0; y < act_height; y++) {
				pixels[color_indexer++].a = source.GetPixelBilinear((float) y / act_width , (float) x / act_height).a;
			}
		}

		res.SetPixels(pixels);
		res.Apply();
		return res;
	}

	public enum TextureTemplate
	{
		he_hole,
		ap_hole,
		default_armor,
	}
}

public class SelectorData
{
	public readonly Sprite default_sprite = GetDefaultSprite(new Vector2Int(30, 30));

	public Dictionary<string, Sprite> sprite_dict = new Dictionary<string, Sprite>();
	public readonly string[] main_options;
	public readonly Sprite[] main_icon;
	public readonly int   [] main_flags;

	public readonly string[] reference_options;
	public readonly Sprite[] reference_icons;
	public readonly int   [] reference_flags;
	public readonly int   [] reference_function_pointers;

	public readonly string[] target_options;
	public readonly Sprite[] target_icons;
	public readonly int   [] target_flags;
	public readonly int   [] target_function_pointers;

	public readonly string[] info_options;
	public readonly Sprite[] info_icons;
	public readonly int   [] info_flags;
	public readonly int   [] info_function_pointers;

	public readonly string[] command_options;
	public readonly Sprite[] command_icons;
	public readonly int   [] command_flags;
	public readonly int   [] command_function_pointers;


	public SelectorData (string p_path) {
		DataStructure data = DataStructure.Load(p_path);
		DataStructure sprite_sources = data.GetChild("SpriteSources");
		string[] sprite_names = sprite_sources.Get<string []>("names");
		Texture2D[] sprite_textures = sprite_sources.Get<Texture2D []>("images");

		sprite_dict.Add("NULL", default_sprite);
		for (int i = 0; i < sprite_names.Length | i < sprite_textures.Length; i++) {
			sprite_dict.Add(sprite_names [i], Sprite.Create(sprite_textures [i], new Rect(0, 0, 30, 30), new Vector2(15, 15)));
		}

		DataStructure icons_ds = data.GetChild("Icons");
		main_icon = ReadSpriteArray(icons_ds.Get<string []>("main"));
		reference_icons = ReadSpriteArray(icons_ds.Get<string []>("reference"));
		target_icons = ReadSpriteArray(icons_ds.Get<string []>("target"));
		info_icons = ReadSpriteArray(icons_ds.Get<string []>("info"));
		command_icons = ReadSpriteArray(icons_ds.Get<string []>("command"));

		DataStructure labels_ds = data.GetChild("Labels");
		main_options = labels_ds.Get<string []>("main");
		reference_options = labels_ds.Get<string []>("reference");
		target_options = labels_ds.Get<string []>("target");
		info_options = labels_ds.Get<string []>("info");
		command_options = labels_ds.Get<string []>("command");
		
		DataStructure flag_ds = data.GetChild("Flags");
		main_flags = flag_ds.Get<int []>("main");
		reference_flags = flag_ds.Get<int []>("reference");
		target_flags = flag_ds.Get<int []>("target");
		info_flags = flag_ds.Get<int []>("info");
		command_flags = flag_ds.Get<int []>("command");

		DataStructure function_ds = data.GetChild("FunctionPointers");
		reference_function_pointers = function_ds.Get<int []>("reference");
		target_function_pointers = function_ds.Get<int []>("target");
		info_function_pointers = function_ds.Get<int []>("info");
		command_function_pointers = function_ds.Get<int []>("command");
	}

	public Sprite[] ReadSpriteArray (string[] names) {
		Sprite[] res = new Sprite[names.Length];
		for (int i=0; i < names.Length; i++) {
			if (sprite_dict.ContainsKey(names [i]))
				res [i] = sprite_dict [names [i]];
			else res [i] = default_sprite;
		}
		return res;
	}

	public static Sprite GetDefaultSprite (Vector2Int size) {
		Texture2D res = new Texture2D(size.x, size.y);
		res.name = "default";
		for (int x=0; x < size.x; x++) {
			for (int y=0; y < size.y; y++) {
				res.SetPixel(x, y, new Color(0, 0, 0, 0));
			}
		}
		res.Apply();
		return Sprite.Create(res, new Rect(0, 0, 30, 30), Vector2.zero);;
	}
}