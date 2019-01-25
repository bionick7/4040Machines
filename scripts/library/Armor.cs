using FileManagement;
using UnityEngine;

public class Armor : ShipPart
{
	public new const string enum_opt = "armor";
	public new const int max_on_ship = 20;

	private ArmorMesh armor_mesh;
	
	public static float thickness = 0;

	private bool initialized = false;

	public Armor (float init_health, GameObject obj, float mass, ArmorMesh p_arm_mesh) : base(init_health, obj, mass) {
		armor_mesh = p_arm_mesh;
	}

	public override void Hit (Bullet hit) {
		Ray bulletray = new Ray(hit.Position, hit.Velocity);
		float thickness = armor_mesh.GetArmorThickness(bulletray);
		if (thickness == 0) return;
		if (hit.ammo.IsKinetic) {
			Texture2D texture = ImpactTextures.ScaleTexture(hit.ammo.dammage_texture,
															(int) Mathf.Ceil(hit.ammo.dammage_texture.width / armor_mesh.mmPpixel.x * 10),
															(int) Mathf.Ceil(hit.ammo.dammage_texture.height / armor_mesh.mmPpixel.y * 10));
			armor_mesh.DrawDammage(bulletray, texture, 1 / thickness);
		}
		else if (hit.ammo.IsExplosive) {
			armor_mesh.DrawDammage(bulletray, hit.ammo.dammage_texture, .02f * hit.ammo.explosion_force / thickness);
		}
		hit.Explode();
		hit.Destroy();
	}

	public override void Hit (Missile hit) {
		base.Hit(hit);
	}

	public override void PausedUpdate () {
		base.PausedUpdate();
		if (!initialized) {
			armor_mesh.PostInitialization();
			initialized = true;
		}

		Ray ray = SceneGlobals.map_camera.ScreenPointToRay(Input.mousePosition);
		thickness = armor_mesh.GetArmorThickness(ray);
	}

	public override void Destroy () {
		base.Destroy();
	}

	public override string Description () {
		return string.Format("Armor {0:0.0} / {1:0.0} HP", HP, InitHP);
	}

	public override DataStructure Save (DataStructure ds) {
		armor_mesh.original_geometry.ToDS(ds);
		ds.Set<ushort>("type", 12);
		ds.Set("texture size", new int [2] { armor_mesh.texture_size.x, armor_mesh.texture_size.y });
		return ds;
	}

	public static Armor GetFromDS (DataStructure data, Ship parent) {
		ArmorMesh.ArmorGeometryData armordata = new ArmorMesh.ArmorGeometryData(
			data.Get<Vector3[]>("key positions"),
			data.Get<float[]>("radii"),
			data.Get<float[]>("thickness"),
			data.Get("sides", 32, quiet:true)
		);
		int[] texture_size = data.Get<int[]> ("texture size");
		ArmorMesh armor_mesh = new ArmorMesh(armordata, new Vector2Int(texture_size[0], texture_size[1]));

		GameObject armor_obj = armor_mesh.armor_obj;
		armor_obj.transform.SetParent(parent.Transform, false);

		// Provisory
		armor_mesh.part = new Armor(1000, armor_obj, 10, armor_mesh);

		BulletCollisionDetection armor_mono = Loader.EnsureComponent<BulletCollisionDetection>(armor_obj);
		armor_mono.Part = armor_mesh.part;

		return armor_mesh.part;
	}
}

public class ArmorMesh
{
	public Mesh Main { get; private set; }
	public float Volume { get; private set; }

	private Mesh outer_collider_mesh;
	private Mesh inner_collider_mesh;

	public ArmorGeometryData original_geometry;
	public ArmorGeometryData geometry;

	private Material[] default_materials = new Material[2];
	private Material thickness_indicator;

	public Armor part;
	public GameObject armor_obj;

	public MeshFilter filter;
	public MeshRenderer renderer;
	public MeshCollider[] colliders;

	public Vector3 dimensions;
	public Vector2 mmPpixel;

	private Texture2D dammage;
	private Texture2D visual_damage;
	private Texture2D raw_thickness;
	private Vector3 offset;

	public Vector2Int texture_size;

	private const float tau = 6.283185f;

	public ArmorMesh (ArmorGeometryData p_geometrydata, Vector2Int p_texture_size) {
		texture_size = p_texture_size;
		geometry = Slice(p_geometrydata);
		original_geometry = p_geometrydata;

		armor_obj = new GameObject("armor");
		offset = (p_geometrydata.keypos [0] + p_geometrydata.keypos [p_geometrydata.size - 1]) / 2;
		armor_obj.transform.position = offset;
		GetMesh();

		filter = armor_obj.AddComponent<MeshFilter>();
		renderer = armor_obj.AddComponent<MeshRenderer>();
		filter.mesh = Main;

		float max_rad = 0;
		for (int i=0; i < geometry.size; i++) {
			if (geometry.radii [i] > max_rad) max_rad = geometry.radii [i];
		}
		dimensions.Set(max_rad, max_rad, Mathf.Abs((geometry.keypos [geometry.size - 1] - geometry.keypos [0]).z));
		mmPpixel.Set(dimensions.z * 1000 / p_texture_size.x, max_rad * tau * 1000 / p_texture_size.y);

		default_materials [0] = Resources.Load("materials/default_armor") as Material;
		default_materials [0].EnableKeyword("_NORMALMAP");
		visual_damage = Globals.impact_textures.GetTexture(ImpactTextures.TextureTemplate.default_armor, p_texture_size.x, p_texture_size.y);
		default_materials [0].SetTexture("_DammageTex", visual_damage);
		default_materials [1] = Resources.Load("materials/armor_inner") as Material;

		thickness_indicator = Resources.Load("materials/armor_thickness") as Material;
		renderer.materials = default_materials;



		raw_thickness = GetRawThicknessTexture();
	}

	public void PostInitialization () {
		MeshCollider col1 = armor_obj.AddComponent<MeshCollider>();
		MeshCollider col2 = armor_obj.AddComponent<MeshCollider>();

		col1.sharedMesh = outer_collider_mesh;
		col2.sharedMesh = inner_collider_mesh;
	}

	private ArmorGeometryData Slice (ArmorGeometryData data) {
		int[] div_segments = new int[data.size - 1];
		int tot_segmentations = 0;
		for (int i=0; i < data.size - 1; i++) {
			// spaces out everything evenly
			float ideal_dist = (data.radii[i] + data.radii[i+1]) * Mathf.PI / data.resolution;
			float whole_d = Vector3.Distance(data.keypos[i], data.keypos[i+1]);
			div_segments[i] = (int) Mathf.Round(whole_d / ideal_dist);
			tot_segmentations += div_segments[i];
		}

		ArmorGeometryData tot_data = new ArmorGeometryData(++tot_segmentations, data.resolution);

		int tot_idx = 0;
		for (int i=0; i < data.size - 1; i++) {
			int div_seg = div_segments[i];
			for (int j=0; j < div_seg; j++) {
				tot_data.keypos [tot_idx] = data.keypos[i] + (data.keypos [i + 1] - data.keypos [i]) * j / div_seg;
				tot_data.radii [tot_idx] = data.radii[i] + (data.radii [i + 1] - data.radii [i]) * j / div_seg;
				tot_data.thicknesses [tot_idx++] = data.thicknesses[i] + (data.thicknesses [i + 1] - data.thicknesses [i]) * j / div_seg;
			}
		}

		tot_data.keypos [tot_idx] = data.keypos[data.size-1];
		tot_data.radii [tot_idx] = data.radii[data.size-1];
		tot_data.thicknesses [tot_idx] = data.thicknesses[data.size-1];
		return tot_data;
	}

	/// <summary> Generates the mesh </summary>
	/// <remarks> Needs 0.028ms per edge (inner and outer) at resolution = 32 </remarks>
	private void GetMesh () {
		Vector3 [] key_pos = geometry.keypos;
		float [] radii = geometry.radii;
		float [] thicknesses = geometry.thicknesses;
		int resolution = geometry.resolution;
		int size = geometry.size;

		if (key_pos.Length != radii.Length || key_pos.Length != thicknesses.Length) {
			DeveloppmentTools.Log("The arrays for armor hav to have the following length pattern: length(key positions) = length(radii) = length(thickness)");
		}

		Main = new Mesh();
		outer_collider_mesh = new Mesh();
		inner_collider_mesh = new Mesh();

		Vector3[] mesh_vertecies = new Vector3[resolution * 2 * size];
		Vector2[] uv1 = new Vector2[resolution * 2 * size];
		int[] outer_triangles = new int[resolution * 6 * (size - 1)];
		int[] inner_triangles = new int[resolution * 6 * (size + 1)];
		Vector3[] outerCol_vertecies = new Vector3[resolution * size];
		Vector3[] innerCol_vertecies = new Vector3[resolution * size];
		Vector2[] col_uv = new Vector2[resolution * size];
		int[] col_triangles = new int[(resolution + 2) * 6 * (size - 1) - 6];

		int collider_tri_index = 0, main_tri_index = 0;
		float max_rad = 0;
		for (int i=0; i < size; i++) {
			if (radii [i] > max_rad) max_rad = radii [i];
		}

		// For uv2 mapping
		float x_step = 1f / size;
		float y_step = 1f / resolution;

		//Vector3 normal;
		int vert1, vert2, vert3, vert4;
		float inclination_ratio = 0;
		int curr_x = 0;
		for (int i=0; i < size; i++) {
		
			if (i != size - 1) { 
				
				// Calculate volume
				float outer_rad1 = radii[i];
				float outer_rad2 = radii[i+1];
				float inner_rad1 = radii[i] - thicknesses[i];
				float inner_rad2 = radii[i+1] - thicknesses[i+1];
				float V = .3333f * Vector3.Distance(key_pos[i], key_pos[i+1]) * (outer_rad1*outer_rad1 + outer_rad2*outer_rad2 - outer_rad1*outer_rad2 
																				-inner_rad1*inner_rad1 - inner_rad2*inner_rad2 + inner_rad1*inner_rad2);
				Volume += V;
				inclination_ratio = (radii[i] - radii[i+1]) / (Vector3.Distance(key_pos[i], key_pos[i+1]) * (key_pos[i].z < key_pos[i+1].z ? 1 : -1));
			}

			if (i != 0) {
				// Subdivisions
				float ideal_dist = (radii[i-1] + radii[i]) * Mathf.PI / resolution;
				float whole_d = Vector3.Distance(key_pos[i-1], key_pos[i]);
				curr_x += (int) Mathf.Round(whole_d / ideal_dist);
			}

			// Calculate vertex positions
			Vector3[] circle_inner = GetPointsInCircle(resolution, radii[i] - thicknesses[i], Vector3.forward);
			Vector3[] circle_outer = GetPointsInCircle(resolution, radii[i], Vector3.forward);
			Vector2[] uv_tot = new Vector2[resolution * 2];

			Vector3 pos = key_pos[i] - offset;

			int init_vert_indx = i*2*resolution;
			int col_init_vert_indx = i * resolution;
			for (int j = 0; j < resolution; j++) {
				// UV Mapping
				uv_tot [j] = new Vector2(i * x_step, j * y_step);

				// Adjust vertex positions
				circle_inner [j] += pos;
				circle_outer [j] += pos;

				// Calculate triangles
				bool j_end = j == resolution-1;
				if (i != size - 1) {
					// Colliders
					vert1 = col_init_vert_indx + j;
					vert2 = col_init_vert_indx + resolution + j;
					vert3 = col_init_vert_indx +			  (j_end ? 0 : j + 1);
					vert4 = col_init_vert_indx + resolution + (j_end ? 0 : j + 1);
					col_triangles [collider_tri_index++] = vert1;
					col_triangles [collider_tri_index++] = vert2;
					col_triangles [collider_tri_index++] = vert3;
					col_triangles [collider_tri_index++] = vert3;
					col_triangles [collider_tri_index++] = vert2;
					col_triangles [collider_tri_index++] = vert4;

					// Inner
					vert1 = init_vert_indx + j;
					vert2 = init_vert_indx + resolution * 2 + j;
					vert3 = init_vert_indx +				  (j_end ? 0 : j + 1);
					vert4 = init_vert_indx + resolution * 2 + (j_end ? 0 : j + 1);
					inner_triangles [main_tri_index++] = vert2;
					inner_triangles [main_tri_index++] = vert1;
					inner_triangles [main_tri_index++] = vert3;
					inner_triangles [main_tri_index++] = vert2;
					inner_triangles [main_tri_index++] = vert3;
					inner_triangles [main_tri_index++] = vert4;
					
					// Outer
					vert1 = init_vert_indx + resolution + j;
					vert2 = init_vert_indx + resolution * 3 + j;
					vert3 = init_vert_indx + resolution + (j_end ? 0 : j + 1);
					vert4 = init_vert_indx + resolution * 3 + (j_end ? 0 : j + 1);
					outer_triangles [main_tri_index-6] = vert1;
					outer_triangles [main_tri_index-5] = vert2;
					outer_triangles [main_tri_index-4] = vert3;
					outer_triangles [main_tri_index-3] = vert3;
					outer_triangles [main_tri_index-2] = vert2;
					outer_triangles [main_tri_index-1] = vert4;
				}
			}

			// Copy vertex positions
			System.Array.Copy(circle_inner, 0, mesh_vertecies, resolution * 2 * i, resolution);
			System.Array.Copy(circle_outer, 0, mesh_vertecies, resolution * (2 * i + 1), resolution);
			System.Array.Copy(circle_inner, 0, innerCol_vertecies, resolution * i, resolution);
			System.Array.Copy(circle_outer, 0, outerCol_vertecies, resolution * i, resolution);
			System.Array.Copy(uv_tot, 0, uv1, resolution * 2 * i, resolution);
			System.Array.Copy(uv_tot, 0, uv1, resolution * (2 * i + 1), resolution);
			System.Array.Copy(uv_tot, 0, col_uv, resolution * i, resolution);
		}

		int vert_indx = 2 * resolution * (size - 1);
		for (int j=0; j < resolution; j++) {
			// Calculate triangles
			bool j_end = j == resolution-1;
			vert1 = j;
			vert2 = resolution + j;
			vert3 = (j_end ? 0 : j + 1);
			vert4 = resolution + (j_end ? 0 : j + 1);
			inner_triangles [main_tri_index++] = vert1;
			inner_triangles [main_tri_index++] = vert2;
			inner_triangles [main_tri_index++] = vert3;
			inner_triangles [main_tri_index++] = vert3;
			inner_triangles [main_tri_index++] = vert2;
			inner_triangles [main_tri_index++] = vert4;

			vert1 = vert_indx + j;
			vert2 = vert_indx + resolution + j;
			vert3 = vert_indx + (j_end ? 0 : j + 1);
			vert4 = vert_indx + resolution + (j_end ? 0 : j + 1);
			inner_triangles [main_tri_index++] = vert1;
			inner_triangles [main_tri_index++] = vert2;
			inner_triangles [main_tri_index++] = vert3;
			inner_triangles [main_tri_index++] = vert3;
			inner_triangles [main_tri_index++] = vert2;
			inner_triangles [main_tri_index++] = vert4;

			// Calculate collider ends
			if (j != 0 && j != resolution - 1) {
				col_triangles [collider_tri_index++] = 0;
				col_triangles [collider_tri_index++] = j;
				col_triangles [collider_tri_index++] = j + 1;

				col_triangles [collider_tri_index++] = resolution * (size - 1);
				col_triangles [collider_tri_index++] = resolution * (size - 1) + j + 1;
				col_triangles [collider_tri_index++] = resolution * (size - 1) + j;
			}
		}

		Main.name = "armor_mesh";
		Main.subMeshCount = 2;
		Main.vertices = mesh_vertecies;
		Main.SetTriangles(outer_triangles, 0);
		Main.SetTriangles(inner_triangles, 1);
		Main.uv = uv1;
		Main.RecalculateNormals();
		
		inner_collider_mesh.name = "inner collider mesh";
		inner_collider_mesh.vertices = innerCol_vertecies;
		inner_collider_mesh.triangles = col_triangles;
		inner_collider_mesh.uv = col_uv;
		inner_collider_mesh.RecalculateNormals();
		
		outer_collider_mesh.name = "outer collider mesh";
		outer_collider_mesh.vertices = outerCol_vertecies;

		for (int i=0; i < outer_collider_mesh.normals.Length; i++) {
			Debug.DrawRay(outer_collider_mesh.vertices [i] + offset, outer_collider_mesh.normals [i], Color.cyan, float.PositiveInfinity);
		}
		
		outer_collider_mesh.triangles = col_triangles;
		outer_collider_mesh.uv = col_uv;

	}

	private static Vector3[] GetPointsInCircle (int sides, float radius, Vector3 normal) {
		Vector3[] res = new Vector3[sides];
		Quaternion rot = Quaternion.FromToRotation(Vector3.forward, normal);
		for (uint i=0; i < sides; i++) {
			res [i] = rot * (new Vector3(Mathf.Cos(i * tau / sides), Mathf.Sin(i * tau / sides), 0) * radius);
		}
		return res;
	}

	private Texture2D GetRawThicknessTexture () {
		Texture2D res = new Texture2D(geometry.size, 1);

		// Get maximum thickness
		float max_thickness = 0;
		for (int i=0; i < geometry.size; i++) {
			if (geometry.thicknesses[i] > max_thickness) {
				max_thickness = geometry.thicknesses [i];
			}
		}

		for (int x=0; x < geometry.size; x++) {
			float relative_thickness = geometry.thicknesses[x] / max_thickness;
			Color color = new Color(relative_thickness, relative_thickness, relative_thickness, 1);
			res.SetPixel(x, 1, color);
		}

		res.Apply();
		return res;
	}

	public void PenetrationView (Vector3 view) {
		renderer.material = thickness_indicator;
		renderer.material.SetVector("_View", new Vector4(view.x, view.y, view.z, 0));
		renderer.material.SetTexture("_Thickness", raw_thickness);
	}

	public void NormalView () {
		renderer.materials = default_materials;
	}

	public float GetArmorThickness (Ray particleMovement) {
		RaycastHit[] hits = Physics.RaycastAll(particleMovement);
		if (hits.Length < 2) return 0;
		return Vector3.Distance(hits [0].point, hits [1].point);
	}

	public bool ArmorIsPresent (Ray ray) {
		RaycastHit hit;
		if (!Physics.Raycast(ray, out hit)) return false;
		Vector2 hitpoituv = hit.textureCoord;
		hitpoituv.x *= visual_damage.width;
		hitpoituv.y *= visual_damage.height;
		return visual_damage.GetPixel((int)hitpoituv.x, (int)hitpoituv.y).a != 1;
	}

	public void DrawDammage (Ray ray, Texture2D image, float mult) {
		RaycastHit hit;
		if (!Physics.Raycast(ray, out hit)) return;
		Vector2 hitpoituv = hit.textureCoord;
		hitpoituv.x *= visual_damage.width;
		hitpoituv.y *= visual_damage.height;

		int x_pos = (int) (hitpoituv.x - image.width / 2);
		int y_pos = (int) (hitpoituv.y - image.height / 2);

		if (x_pos - image.width / 2 < 0 || x_pos + image.width / 2 > visual_damage.width) return;
		if (y_pos - image.height / 2 < 0 || y_pos + image.height / 2 > visual_damage.height) return;

		Color col1;
		Color col2;

		for (int x = 0; x < image.width; x++) {
			for (int y = 0; y < image.height; y++) {
				col1 = visual_damage.GetPixel(x + x_pos, y + y_pos);
				col2 = image.GetPixel(x, y);
				col1.a = 1 - (1 - col2.a * mult) * (1 - col1.a);
				visual_damage.SetPixel(x + x_pos, y + y_pos, col1);
			}
		}
		visual_damage.Apply();

		renderer.material.SetTexture("_DammageTex", visual_damage);
	}

	public struct ArmorGeometryData
	{
		/// <summary> The key positions of the cylinder edges </summary>
		public Vector3[] keypos;
		/// <summary> The radii of the circles/edges </summary>
		public float [] radii;
		/// <summary> The difference between the radii of the inner edges and the outer edges </summary>
		public float[] thicknesses;
		/// <summary> The key positions of the cylinder edges </summary>
		public int resolution;

		public int size;

		public ArmorGeometryData (int p_size, int presolution) {
			size = p_size;
			keypos = new Vector3 [p_size];
			radii = new float [p_size];
			thicknesses = new float [p_size];
			resolution = presolution;
		}

		public ArmorGeometryData (Vector3[] pkeypos, float[] pradii, float[] pthicknesses, int presolution) {
			size = pkeypos.Length;
			resolution = presolution;
			if (pradii.Length != size || pthicknesses.Length != size) {
				DeveloppmentTools.Log("The arrays for armor hav to have the following length pattern: length(key positions) = length(radii) = length(thickness)");
				keypos = new Vector3 [0];
				radii = thicknesses = new float [0];
				return;
			}
			keypos = pkeypos;
			radii = pradii;
			thicknesses = pthicknesses;
		}

		public DataStructure ToDS (DataStructure ds) {
			ds.Set("key positions", keypos);
			ds.Set("radii", radii);
			ds.Set("thickness", thicknesses);
			ds.Set("sides", resolution);
			return ds;
		}
	}
}
