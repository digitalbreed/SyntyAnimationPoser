using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace digitalbreed {

	/// <summary>
	/// Editor window for Synty Animation Poser tool.
	/// Allows selecting animation packs and art packs, then placing posed characters in the scene.
	/// </summary>
	public class SyntyAnimationPoserWindow : EditorWindow {

		private readonly struct LabelWidthScope : IDisposable {
			private readonly float oldLabelWidth;

			public LabelWidthScope(float labelWidth) {
				oldLabelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = labelWidth;
			}

			public void Dispose() {
				EditorGUIUtility.labelWidth = oldLabelWidth;
			}
		}

		private static float GetHalfLabelWidth() {
			return Mathf.Max(160f, (EditorGUIUtility.currentViewWidth - 60f) * 0.5f);
		}

	[Serializable]
	private class PackItem {
		public string guid;
		public string name;
		public bool enabled;
		public bool requiresStrictMaterialMatching = false;

		public PackItem(string guid, string name) {
			this.guid = guid;
			this.name = name;
			this.enabled = true;
			this.requiresStrictMaterialMatching = false;
		}

		public PackItem(string guid, string name, bool requiresStrictMaterialMatching) {
			this.guid = guid;
			this.name = name;
			this.enabled = true;
			this.requiresStrictMaterialMatching = requiresStrictMaterialMatching;
		}
	}

		[Serializable]
		private class PackData {
			public List<PackItem> items = new();
		}

		// The GUIDs are from the respective Polygon subfolders
		private static readonly PackItem[] DEFAULT_ANIMATION_PACKS = new PackItem[] {
			new PackItem("eadd5ff51afa2d14db9ef40757dc3736", "Base Locomotion"),
			new PackItem("4d0768993258c6843a5f3b79a207c28b", "Bow Combat"),
			new PackItem("72e4b81c91237f044a9eb7c358352031", "Emotes And Taunts"),
			new PackItem("3b46f33d28bc6ee4d95329b65350ba70", "Goblin Locomotion"),
			new PackItem("8e1adc12bccbc234ba5daab4b559b8de", "Idles"),
			new PackItem("ba81f3c3b0615db498503dc7448a7d03", "Sword Combat"),
			new PackItem("ccced8cf7128c6843a1a251eee9f0547", "Dog Pack")
		};

		// The GUIDs are from the respective Prefabs/Characters folders to narrow down the search;
		// if fixed scale directories existed, these were used.
		private static readonly PackItem[] DEFAULT_ART_PACKS = new PackItem[] {
			new PackItem("2b5ead0f74558cd4e9d5a3486a80872b", "Adventure", true),
			new PackItem("c211e5d1bc9cead4987f802157fb871d", "Ancient Egypt"),
			new PackItem("7a5f85716bd68db4ebfcd5e977c497e6", "Ancient Empire"),
			new PackItem("3d558d45b3c2b7545995f505580ffce5", "Apocalypse Pack"),
			new PackItem("81040494bf85b4e488e122e44230c955", "Apocalypse Wasteland"),
			new PackItem("4373c831acb38f443bcf9d2b346fb2eb", "Battle Royale Pack", true),
			new PackItem("2be6f6cb3be77cd43956b46e33092afa", "Boss Zombies"),
			new PackItem("0210de768f7e2f3418995a3881f2794b", "Casino"),
			new PackItem("2cbd4474c9bc1734aa58e2a6a4db46a3", "City Characters"),
			new PackItem("3057162e47aef7a4a9ba89964c82facb", "City Pack"),
			new PackItem("0b3bbad1452060f43be85b6c521f4584", "City Zombies Pack", true),
			new PackItem("692c2a6ec2af8d7498a283c37a5ef387", "Construction Pack"),
			new PackItem("af1e859462282244cbbe0ede7533e29d", "Dark Fantasy"),
			new PackItem("b888bf17c9e95eb439a35aa8f055c208", "Dark Fortress"),
			new PackItem("cae0bde811e976f42b443c34e8e1d99e", "Dog Pack"),
			new PackItem("a3867745e2c0fde4fb1b6797e4e47a9d", "Dungeon Pack"),
			new PackItem("3456349c88ea2e244820e64d07c038a6", "Dungeon Realms"),
			new PackItem("6daaccc3784923849b102dc2c5030f02", "Elven Realm"),
			new PackItem("18f54c36cdae15a469c52b17088ae454", "Explorer Kit"),
			new PackItem("3e0e8d55fc688f04b95d4bc16bd766f0", "Fantasy Characters Pack"),
			new PackItem("261c87575100d8a419e8a2a56681257c", "Fantasy Kingdom Pack"),
			new PackItem("b672fffd08f0bb342b4b50f4cd071e89", "Fantasy Rivals Pack"),
			new PackItem("07e7f55fac23c5845b662d5d1f0d5cb7", "Farm Pack"),
			new PackItem("d6a080b54d1f4e5419e63bb00618d9f7", "Gang Warfare"),
			new PackItem("7172a832b1d9fe342a9a9e92672ef719", "Goblin War Pack"),
			new PackItem("a1b4e08b033be8547938b2756273039c", "Heist Pack"),
			new PackItem("a74edf19c79900449928210a8735b1ef", "Horror Asylum"),
			new PackItem("9bc77fa77c0dcac43aee242b8a67167c", "Horror Carnival"),
			new PackItem("b2eea5842befe134e9ed1e1be9584e6c", "Horror Mansion"),
			new PackItem("0a3173329b0b29a4f9a9833c0598cb27", "Kaiju"),
			new PackItem("b0e418d353199dd47a4daf90ab8ff2e1", "Kids Pack"),
			new PackItem("eb1e6388502db1047a63eb5f93133395", "Knights Pack"),
			new PackItem("98a24e58dbbad58468094498e23b8c4e", "Mech Pack (Pilots)"),
			new PackItem("2523111f43bffc24596c6753050bac2a", "Mech Pack (Mech Prefabs)"),
			new PackItem("d9b456971d020e04b8008a5a6df48106", "Military Pack"),
			new PackItem("191b847b6946a114dab1619f410dd59a", "Modular Fantasy Hero Characters (Presets)"),
			new PackItem("fecb2040631c63c4ea46c50fe8e09709", "Nightclubs"),
			new PackItem("7e943b84df3af6d49a6434848552487a", "Office Pack"),
			new PackItem("61183204b532ad7468bfbae80df245be", "Pirate Pack"),
			new PackItem("1287da7c34e096045831543e16bfab45", "Police Station"),
			new PackItem("078b508e123894b448f4fdc2fa73c2ca", "Pro Racer"),
			new PackItem("9caeb5f285f269547b4d043e7e46eb55", "Prototype Pack"),
			new PackItem("cd42cc1bf20b1134a99947ce257985a4", "Samurai Pack", true),
			new PackItem("10b68e916d548564c9d5adf010f1d6b4", "Samurai Empire"),
			new PackItem("1ed3a10d3a1a60543b5d0c9b730f62f7", "Santa Pack"),
			new PackItem("b358ad0a90e4fd140bbf267cc42a8050", "Sci-Fi City Pack"),
			new PackItem("0e62d5c46c4e5ba48928e0738019ee07", "Sci-Fi Cyber City"),
			new PackItem("b086a73d9454a6a429d9e47370a124a0", "Sci-Fi Horror"),
			new PackItem("6011f8cc8c963964fb1c33a77668fb3b", "Sci-Fi Space Pack"),
			new PackItem("f490d5a9d11b86f4791e3ee8ff3cb28a", "Sci-Fi Worlds Pack"),
			new PackItem("89c563bf718c4244a9e587c320710d5f", "Shops Pack"),
			new PackItem("084f525705bbc4346b754570cd2bb5cd", "Snow Kit"),
			new PackItem("5de42264beed8b34daa86b412855e844", "Spy Kit"),
			new PackItem("cfbf8ec36351f9d4a92f1eba0c678015", "Street Racer"),
			new PackItem("1d9a9289d1f32bc4f9e0de15027fa2ea", "Town Pack"),
			new PackItem("fc361c22505cfa6408dac29b2c3e013c", "Vikings Pack"),
			new PackItem("2f512a6b32b3c304ea9dbe15b16f5293", "Viking Realm"),
			new PackItem("380a471ac8b68f84595ec57a92ff2fae", "War Map - WWI"),
			new PackItem("017773923fd155c4aa80644f94e52b86", "War Pack"),
			new PackItem("5b14fa733cb30994788575af57f25ccc", "Werewolf"),
			new PackItem("3fb2fad9fd64e6d419d8bcba139dc6ee", "Western Frontier Pack"),
			new PackItem("00ffec4e2efc9cd4e9bf55016ab0fa44", "Western Pack")
		};

		// Character prefab name filters (positive/negative checklist)
		private static readonly string[] POSITIVE_PREFIXES = {
			"SM_Chr_",
			"Character_",
			"Zombie_",
			"Unity_SK_Animals_Dog_",
			"Chr_",
			"Chr_BR_"
		};
		private static readonly string[] NEGATIVE_PREFIXES = {
			"SM_Chr_Attach_",
			"SM_Chr_Attachment_",
			"SM_Chr_Hair_",
			"SM_Chr_Patch_",
			"Unity_SK_Animals_Dog_ALL_01",
			"Chr_Attach_",
			"Chr_Hair_",
			"SM_Chr_Inspector_",
			"SM_Chr_Pilot_Torso_01",
			"SM_Chr_Carny_"
		};

		[SerializeField] private PackData animationPacks = new PackData();
		[SerializeField] private PackData artPacks = new PackData();
		[SerializeField] private Transform parentTransform;
		[SerializeField] private bool isStarted = false;
		[SerializeField] private bool needsRescan = true;
		[SerializeField] private bool animationPacksFoldout = true;
		[SerializeField] private bool artPacksFoldout = true;
		[SerializeField] private bool placementOptionsFoldout = true;
		[SerializeField] private bool filterOptionsFoldout = true;

		[SerializeField] private bool isScanning = false;
		[SerializeField] private float scanProgress01 = 0f;
		[SerializeField] private string scanStatus = "";

		[SerializeField] private bool useCollisionNormal = false;
		[SerializeField] private Vector3 alignmentVector = Vector3.up;
		[SerializeField] private bool randomYRotation = true;
		[SerializeField] private bool rotateHead = false;
		[SerializeField] private float headHorizontalRange = 0f;
		[SerializeField] private float headVerticalRange = 0f;
		[SerializeField] private bool randomMaterial = false;
		[SerializeField] private bool startAfterScan = false;

		[SerializeField] private string animationNameFilter = "";
		[SerializeField] private string characterNameFilter = "";

		private IEnumerator scanEnumerator;

		// Cached data
		private List<AnimationClip> cachedAnimationClips = new();
		private List<GameObject> cachedCharacterPrefabs = new();
		private Dictionary<GameObject, PackItem> prefabToPackMap = new Dictionary<GameObject, PackItem>();
		private string lastSelectionHash = "";

		private Vector2 animationPacksScroll;
		private Vector2 artPacksScroll;

		private const string PREFS_PREFIX = "SyntyAnimationPoser.";

		[MenuItem("Tools/digitalbreed/Synty Animation Poser")]
		public static void ShowWindow() {
			GetWindow<SyntyAnimationPoserWindow>("Synty Animation Poser");
		}

		private void OnEnable() {
			// Initialize default packs if empty
			if (animationPacks.items.Count == 0) {
				animationPacks.items.AddRange(DEFAULT_ANIMATION_PACKS.Select(p => new PackItem(p.guid, p.name) {
					enabled = p.enabled,
					requiresStrictMaterialMatching = p.requiresStrictMaterialMatching
				}));
			}
			if (artPacks.items.Count == 0) {
				artPacks.items.AddRange(DEFAULT_ART_PACKS.Select(p => new PackItem(p.guid, p.name) {
					enabled = p.enabled,
					requiresStrictMaterialMatching = p.requiresStrictMaterialMatching
				}));
			}

			// Load persisted settings
			LoadSettings();

			// Subscribe to scene view for click detection
			SceneView.duringSceneGui += OnSceneGUI;

			// Only reset state if this is a fresh window (no cached data and not scanning)
			// This prevents resetting state when window is re-enabled after being disabled (e.g., scene view maximized)
			bool isFirstTime = cachedAnimationClips.Count == 0 && cachedCharacterPrefabs.Count == 0 && !isScanning && string.IsNullOrEmpty(lastSelectionHash);
			if (isFirstTime) {
				isStarted = false;
				needsRescan = true;
				scanStatus = "Ready to rescan.";
			}
			// If scanning was in progress, ensure the update subscription is still active
			// (it should be, but check to be safe)
			else if (isScanning && scanEnumerator != null) {
				// Ensure the scan step is subscribed (might have been lost if window was destroyed/recreated)
				EditorApplication.update -= ScanStep; // Remove first to avoid duplicates
				EditorApplication.update += ScanStep;
			}
		}

		private void OnDisable() {
			SaveSettings();

			SceneView.duringSceneGui -= OnSceneGUI;
		}

		private void OnGUI() {
			EditorGUILayout.Space(10);

			bool selectionChangedThisFrame = false;

			// Animation Packs foldout
			EditorGUI.BeginChangeCheck();
			animationPacksFoldout = EditorGUILayout.Foldout(animationPacksFoldout, "Animations packs", true);
			if (animationPacksFoldout) {
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				animationPacksScroll = EditorGUILayout.BeginScrollView(animationPacksScroll, GUILayout.Height(150));
				EditorGUILayout.Space(5);

				EditorGUI.BeginChangeCheck();
				for (int i = 0; i < animationPacks.items.Count; i++) {
					EditorGUILayout.BeginHorizontal();
					animationPacks.items[i].enabled = EditorGUILayout.Toggle(animationPacks.items[i].enabled, GUILayout.Width(20));
					EditorGUILayout.LabelField(animationPacks.items[i].name);
					EditorGUILayout.EndHorizontal();
				}
				if (EditorGUI.EndChangeCheck()) {
					selectionChangedThisFrame = true;
				}

				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
			}
			if (EditorGUI.EndChangeCheck()) {
				SaveSettings();
			}

			EditorGUILayout.Space(10);

			// POLYGON Art Packs foldout
			EditorGUI.BeginChangeCheck();
			artPacksFoldout = EditorGUILayout.Foldout(artPacksFoldout, "POLYGON art packs", true);
			if (artPacksFoldout) {
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Select all")) {
					for (int i = 0; i < artPacks.items.Count; i++) {
						artPacks.items[i].enabled = true;
					}
					selectionChangedThisFrame = true;
				}
				if (GUILayout.Button("Deselect all")) {
					for (int i = 0; i < artPacks.items.Count; i++) {
						artPacks.items[i].enabled = false;
					}
					selectionChangedThisFrame = true;
				}
				EditorGUILayout.EndHorizontal();
				artPacksScroll = EditorGUILayout.BeginScrollView(artPacksScroll, GUILayout.Height(150));
				EditorGUILayout.Space(5);

				EditorGUI.BeginChangeCheck();
				for (int i = 0; i < artPacks.items.Count; i++) {
					EditorGUILayout.BeginHorizontal();
					artPacks.items[i].enabled = EditorGUILayout.Toggle(artPacks.items[i].enabled, GUILayout.Width(20));
					EditorGUILayout.LabelField(artPacks.items[i].name);
					EditorGUILayout.EndHorizontal();
				}
				if (EditorGUI.EndChangeCheck()) {
					selectionChangedThisFrame = true;
				}

				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
			}
			if (EditorGUI.EndChangeCheck()) {
				SaveSettings();
			}

			if (selectionChangedThisFrame) {
				needsRescan = true;
				isStarted = false;
				SaveSettings();
			}

			EditorGUILayout.Space(10);

			// Placement Options foldout
			EditorGUI.BeginChangeCheck();
			placementOptionsFoldout = EditorGUILayout.Foldout(placementOptionsFoldout, "Placement Options", true);
			if (placementOptionsFoldout) {
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.Space(5);

				parentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform", parentTransform, typeof(Transform), true);

				EditorGUILayout.Space(5);

				useCollisionNormal = EditorGUILayout.Toggle("Use Collision Normal", useCollisionNormal);

				using (new EditorGUI.DisabledScope(useCollisionNormal)) {
					alignmentVector = EditorGUILayout.Vector3Field("Alignment Vector", alignmentVector);
				}

				randomYRotation = EditorGUILayout.Toggle("Random Y Rotation", randomYRotation);

				EditorGUILayout.Space(6);
				randomMaterial = EditorGUILayout.Toggle("Random Material", randomMaterial);

				EditorGUILayout.Space(6);
				rotateHead = EditorGUILayout.Toggle("Rotate head", rotateHead);
				using (new EditorGUI.DisabledScope(!rotateHead)) {
					using (new LabelWidthScope(GetHalfLabelWidth())) {
						headHorizontalRange = EditorGUILayout.Slider("Head horizontal range (yaw)", headHorizontalRange, 0f, 120f);
						headVerticalRange = EditorGUILayout.Slider("Head vertical range (pitch)", headVerticalRange, 0f, 120f);
					}
				}

				EditorGUILayout.EndVertical();
			}
			if (EditorGUI.EndChangeCheck()) {
				SaveSettings();
			}

			EditorGUILayout.Space(10);

			// Filter Options foldout
			EditorGUI.BeginChangeCheck();
			filterOptionsFoldout = EditorGUILayout.Foldout(filterOptionsFoldout, "Filter Options", true);
			if (filterOptionsFoldout) {
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.Space(5);

				animationNameFilter = EditorGUILayout.TextField("Animation name contains", animationNameFilter);
				const float quickButtonWidth = 52f;
				const float quickButtonHeight = 18f;

				bool quickFillPressed = false;

				EditorGUILayout.Space(2);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(" ");
				if (GUILayout.Button("idle", GUILayout.Width(quickButtonWidth), GUILayout.Height(quickButtonHeight))) {
					animationNameFilter = AddFilterToken(animationNameFilter, "idle");
					animationNameFilter = AddFilterToken(animationNameFilter, "_IDL_");
					quickFillPressed = true;
				}
				if (GUILayout.Button("emotes", GUILayout.Width(quickButtonWidth), GUILayout.Height(quickButtonHeight))) {
					animationNameFilter = AddFilterToken(animationNameFilter, "_EMOT_");
					quickFillPressed = true;
				}
				if (GUILayout.Button("walk", GUILayout.Width(quickButtonWidth), GUILayout.Height(quickButtonHeight))) {
					animationNameFilter = AddFilterToken(animationNameFilter, "walk");
					quickFillPressed = true;
				}
				if (GUILayout.Button("run", GUILayout.Width(quickButtonWidth), GUILayout.Height(quickButtonHeight))) {
					animationNameFilter = AddFilterToken(animationNameFilter, "run");
					quickFillPressed = true;
				}
				if (GUILayout.Button("X", GUILayout.Width(24), GUILayout.Height(quickButtonHeight))) {
					animationNameFilter = "";
					quickFillPressed = true;
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space(6);
				characterNameFilter = EditorGUILayout.TextField("Character name contains", characterNameFilter);

				// Character quick-fill buttons
				EditorGUILayout.Space(2);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(" ");
				if (GUILayout.Button("male", GUILayout.Width(quickButtonWidth), GUILayout.Height(quickButtonHeight))) {
					characterNameFilter = AddFilterToken(characterNameFilter, "male");
					quickFillPressed = true;
				}
				if (GUILayout.Button("female", GUILayout.Width(quickButtonWidth), GUILayout.Height(quickButtonHeight))) {
					characterNameFilter = AddFilterToken(characterNameFilter, "female");
					quickFillPressed = true;
				}
				if (GUILayout.Button("zombie", GUILayout.Width(quickButtonWidth), GUILayout.Height(quickButtonHeight))) {
					characterNameFilter = AddFilterToken(characterNameFilter, "zombie");
					quickFillPressed = true;
				}
				if (GUILayout.Button("X", GUILayout.Width(24), GUILayout.Height(quickButtonHeight))) {
					characterNameFilter = "";
					quickFillPressed = true;
				}
				EditorGUILayout.EndHorizontal();

				if (quickFillPressed) {
					GUI.FocusControl(null);
					SaveSettings();
				}

				EditorGUILayout.EndVertical();
			}
			if (EditorGUI.EndChangeCheck()) {
				SaveSettings();
			}

			EditorGUILayout.Space(10);

			// Start button
			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = 14;
			buttonStyle.fontStyle = FontStyle.Bold;
			buttonStyle.fixedHeight = 40;

			string currentHash = GetSelectionHash();
			bool selectionMismatch = currentHash != lastSelectionHash;
			bool rescanNeeded = needsRescan || selectionMismatch || cachedAnimationClips.Count == 0 || cachedCharacterPrefabs.Count == 0;

			EditorGUILayout.BeginHorizontal();
			using (new EditorGUI.DisabledScope(isScanning)) {
				string startLabel = isStarted ? "Stop" : "Start";
				if (GUILayout.Button(startLabel, buttonStyle)) {
					if (isStarted) {
						isStarted = false;
						startAfterScan = false;
					} else {
						if (rescanNeeded) {
							startAfterScan = true;
							StartScan(currentHash);
						} else {
							isStarted = true;
						}
					}
				}
			}

			using (new EditorGUI.DisabledScope(isScanning)) {
				if (GUILayout.Button("Rescan", GUILayout.Width(80), GUILayout.Height(buttonStyle.fixedHeight))) {
					startAfterScan = isStarted;
					StartScan(currentHash);
				}
			}
			EditorGUILayout.EndHorizontal();

			if (!isScanning && rescanNeeded) {
				EditorGUILayout.HelpBox("Rescan is needed to refresh the available assets from your selected packs. Start will automatically rescan the first time.", MessageType.Info);
			}

			if (isScanning) {
				EditorGUILayout.Space(5);
				Rect r = EditorGUILayout.GetControlRect(false, 18);
				EditorGUI.ProgressBar(r, Mathf.Clamp01(scanProgress01), string.IsNullOrWhiteSpace(scanStatus) ? "Scanning..." : scanStatus);
			}

			if (!isScanning && isStarted) {
				EditorGUILayout.Space(5);
				EditorGUILayout.HelpBox($"Ready! Click in Scene View to place characters.\nAnimations: {cachedAnimationClips.Count}\nCharacters: {cachedCharacterPrefabs.Count}", MessageType.Info);
			}
		}

		private string GetSelectionHash() {
			var enabledAnimationPacks = animationPacks.items.Where(p => p.enabled).Select(p => p.guid).OrderBy(g => g);
			var enabledArtPacks = artPacks.items.Where(p => p.enabled).Select(p => p.guid).OrderBy(g => g);
			return string.Join("|", enabledAnimationPacks) + "||" + string.Join("|", enabledArtPacks);
		}

		private void StartScan(string expectedHashAfterScan) {
			StopScan();

			isScanning = true;
			scanProgress01 = 0f;
			scanStatus = "Preparing scan...";

			scanEnumerator = ScanAssetsCoroutine(expectedHashAfterScan);
			EditorApplication.update += ScanStep;
		}

		private void StopScan() {
			if (scanEnumerator != null) {
				EditorApplication.update -= ScanStep;
				scanEnumerator = null;
			}
			isScanning = false;
			scanProgress01 = 0f;
		}

		private void ScanStep() {
			if (scanEnumerator == null) {
				EditorApplication.update -= ScanStep;
				return;
			}

			double start = EditorApplication.timeSinceStartup;
			const double budgetSeconds = 0.006;

			try {
				while (EditorApplication.timeSinceStartup - start < budgetSeconds) {
					if (!scanEnumerator.MoveNext()) {
						scanEnumerator = null;
						EditorApplication.update -= ScanStep;
						isScanning = false;
						scanProgress01 = 1f;
						if (string.IsNullOrWhiteSpace(scanStatus)) {
							scanStatus = "Scan complete.";
						}
						if (startAfterScan) {
							isStarted = true;
							startAfterScan = false;
						}
						Repaint();
						return;
					}
				}
			} catch (Exception ex) {
				Debug.LogWarning($"Synty Animation Poser: Scan failed: {ex.Message}");
				scanEnumerator = null;
				EditorApplication.update -= ScanStep;
				isScanning = false;
				scanStatus = "Scan failed (see Console).";
			} finally {
				Repaint();
			}
		}

		private IEnumerator ScanAssetsCoroutine(string expectedHashAfterScan) {
			List<AnimationClip> newClips = new();
			List<GameObject> newPrefabs = new();
			Dictionary<GameObject, PackItem> newPrefabToPackMap = new();

			var enabledAnimPacks = animationPacks.items.Where(p => p.enabled).ToList();
			var enabledArtPacks = artPacks.items.Where(p => p.enabled).ToList();

			int totalStages = Mathf.Max(1, enabledAnimPacks.Count + enabledArtPacks.Count);
			int stage = 0;

			// Scan animation packs
			foreach (var pack in enabledAnimPacks) {
				stage++;
				scanStatus = $"Scanning animations: {pack.name}";
				scanProgress01 = (float)stage / totalStages;

				string assetPath = AssetDatabase.GUIDToAssetPath(pack.guid);
				if (string.IsNullOrEmpty(assetPath)) {
					Debug.LogWarning($"Animation pack '{pack.name}' (GUID: {pack.guid}) not found in project.");
					yield return null;
					continue;
				}

				string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { assetPath });
				for (int i = 0; i < guids.Length; i++) {
					string clipPath = AssetDatabase.GUIDToAssetPath(guids[i]);
					AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
					if (clip != null) {
						newClips.Add(clip);
					}

					if ((i % 25) == 0) {
						yield return null;
					}
				}

				yield return null;
			}

			// Scan art packs for character prefabs
			foreach (var pack in enabledArtPacks) {
				stage++;
				scanStatus = $"Scanning prefabs: {pack.name}";
				scanProgress01 = (float)stage / totalStages;

				string assetPath = AssetDatabase.GUIDToAssetPath(pack.guid);
				if (string.IsNullOrEmpty(assetPath)) {
					Debug.LogWarning($"Art pack '{pack.name}' (GUID: {pack.guid}) not found in project.");
					yield return null;
					continue;
				}

				// Prefab search (faster & less noisy than t:GameObject)
				string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { assetPath });
				for (int i = 0; i < guids.Length; i++) {
					string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
					GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
					if (prefab != null && IsValidCharacterPrefab(prefab.name)) {
						newPrefabs.Add(prefab);
						// Map prefab to its pack
						newPrefabToPackMap[prefab] = pack;
					}

					if ((i % 25) == 0) {
						yield return null;
					}
				}

				yield return null;
			}

			// Commit results
			cachedAnimationClips = newClips.Distinct().ToList();
			cachedCharacterPrefabs = newPrefabs.Distinct().ToList();
			prefabToPackMap = newPrefabToPackMap;
			lastSelectionHash = expectedHashAfterScan;
			needsRescan = false;

			scanStatus = $"Scan complete. Animations: {cachedAnimationClips.Count}, Characters: {cachedCharacterPrefabs.Count}";
			Debug.Log($"Synty Animation Poser: {scanStatus}");
		}

		private bool IsValidCharacterPrefab(string prefabName) {
			bool matchesPositive = false;
			foreach (var prefix in POSITIVE_PREFIXES) {
				if (prefabName.StartsWith(prefix)) {
					matchesPositive = true;
					break;
				}
			}

			if (!matchesPositive) {
				return false;
			}

			foreach (var prefix in NEGATIVE_PREFIXES) {
				if (prefabName.StartsWith(prefix)) {
					return false;
				}
			}

			return true;
		}

		private List<AnimationClip> GetFilteredAnimationClips() {
			if (cachedAnimationClips == null || cachedAnimationClips.Count == 0) {
				return new List<AnimationClip>();
			}

			var (inclusionTokens, exclusionTokens) = ParseFilterTokensWithExclusions(animationNameFilter);
			if (inclusionTokens.Count == 0 && exclusionTokens.Count == 0) {
				return cachedAnimationClips;
			}

			return cachedAnimationClips
				.Where(c => {
					if (c == null) {
						return false;
					}
					if (inclusionTokens.Count > 0 && !inclusionTokens.Any(t => c.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) {
						return false;
					}
					if (exclusionTokens.Any(t => c.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) {
						return false;
					}
					return true;
				})
				.ToList();
		}

		private List<GameObject> GetFilteredCharacterPrefabs() {
			if (cachedCharacterPrefabs == null || cachedCharacterPrefabs.Count == 0) {
				return new();
			}

			var (inclusionTokens, exclusionTokens) = ParseFilterTokensWithExclusions(characterNameFilter);
			if (inclusionTokens.Count == 0 && exclusionTokens.Count == 0) {
				return cachedCharacterPrefabs;
			}

			return cachedCharacterPrefabs
				.Where(p => {
					if (p == null) {
						return false;
					}
					if (inclusionTokens.Count > 0 && !inclusionTokens.Any(t => p.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) {
						return false;
					}
					if (exclusionTokens.Any(t => p.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)) {
						return false;
					}
					return true;
				})
				.ToList();
		}

		private static List<string> ParseFilterTokens(string raw) {
			if (string.IsNullOrWhiteSpace(raw)) {
				return new();
			}

			// Split by comma, trim, drop empty, de-dupe (case-insensitive)
			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string part in raw.Split(',')) {
				string t = (part ?? "").Trim();
				if (t.Length == 0) {
					continue;
				}
				set.Add(t);
			}

			return set.ToList();
		}

		private static (List<string> inclusionTokens, List<string> exclusionTokens) ParseFilterTokensWithExclusions(string raw) {
			var inclusionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var exclusionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (!string.IsNullOrWhiteSpace(raw)) {
				foreach (string part in raw.Split(',')) {
					string t = (part ?? "").Trim();
					if (t.Length == 0) {
						continue;
					}

					// Check if token starts with "!" for exclusion
					if (t.StartsWith("!")) {
						string exclusionToken = t.Substring(1).Trim();
						if (exclusionToken.Length > 0) {
							exclusionSet.Add(exclusionToken);
						}
					} else {
						inclusionSet.Add(t);
					}
				}
			}

			return (inclusionSet.ToList(), exclusionSet.ToList());
		}

		private static string AddFilterToken(string existing, string token) {
			string t = (token ?? "").Trim();
			if (t.Length == 0) {
				return existing ?? "";
			}

			List<string> tokens = ParseFilterTokens(existing);
			if (tokens.Any(x => x.Equals(t, StringComparison.OrdinalIgnoreCase))) {
				return string.Join(", ", tokens);
			}

			tokens.Add(t);
			return string.Join(", ", tokens);
		}

		private static Transform FindHeadTransform(GameObject root) {
			if (root == null) {
				return null;
			}

			Animator animator = root.GetComponentInChildren<Animator>();
			if (animator != null && animator.isHuman) {
				Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
				if (head != null) {
					return head;
				}
			}

			string[] headNameContains = { "head" }; // TODO: check if there are any other names used
			foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) {
				string n = t.name ?? "";
				if (headNameContains.Any(k => n.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)) {
					return t;
				}
			}

			return null;
		}

		private void ApplyRandomHeadRotation(GameObject instance, float horizontalRange, float verticalRange) {
			Transform head = FindHeadTransform(instance);
			if (head == null) {
				return;
			}

			// Range is total span; apply +/- half-range around current pose.
			float yaw = UnityEngine.Random.Range(-horizontalRange * 0.5f, horizontalRange * 0.5f);   // Y axis
			float pitch = UnityEngine.Random.Range(-verticalRange * 0.5f, verticalRange * 0.5f);     // X axis

			if (Mathf.Approximately(yaw, 0f) && Mathf.Approximately(pitch, 0f)) {
				return;
			}

			// Apply in local space as an additive tweak.
			head.localRotation = head.localRotation * Quaternion.Euler(pitch, yaw, 0f);
			EditorUtility.SetDirty(head);
		}

		private void ApplyRandomMaterial(GameObject instance, GameObject prefab) {
			if (instance == null || prefab == null) {
				return;
			}

			// Look up which pack this prefab belongs to from the dictionary
			prefabToPackMap.TryGetValue(prefab, out PackItem pack);
			bool requiresStrictMatching = pack != null && pack.requiresStrictMaterialMatching;

			// Get prefab asset path
			string prefabPath = AssetDatabase.GetAssetPath(prefab);
			if (string.IsNullOrEmpty(prefabPath)) {
				Debug.LogWarning($"Synty Animation Poser: Could not find asset path for prefab '{prefab.name}'");
				return;
			}

			// Get all renderers from the prefab (to find original materials)
			Renderer[] prefabRenderers = prefab.GetComponentsInChildren<Renderer>(false);
			Renderer[] instanceRenderers = instance.GetComponentsInChildren<Renderer>(false);

			if (prefabRenderers.Length == 0 || instanceRenderers.Length == 0) {
				return;
			}

			// Create a mapping of renderer indices (assuming same hierarchy structure)
			// We'll match renderers by their position in the hierarchy
			for (int i = 0; i < Mathf.Min(prefabRenderers.Length, instanceRenderers.Length); i++) {
				Renderer prefabRenderer = prefabRenderers[i];
				Renderer instanceRenderer = instanceRenderers[i];

				if (prefabRenderer == null || instanceRenderer == null) {
					continue;
				}

				Material[] prefabMaterials = prefabRenderer.sharedMaterials;
				Material[] newMaterials = new Material[prefabMaterials.Length];

				for (int matIndex = 0; matIndex < prefabMaterials.Length; matIndex++) {
					Material originalMaterial = prefabMaterials[matIndex];
					if (originalMaterial == null) {
						newMaterials[matIndex] = null;
						continue;
					}

					// Get the asset path of the original material
					string materialPath = AssetDatabase.GetAssetPath(originalMaterial);
					if (string.IsNullOrEmpty(materialPath)) {
						// Material not found in assets, keep original
						newMaterials[matIndex] = originalMaterial;
						continue;
					}

					string materialFolder = Path.GetDirectoryName(materialPath).Replace('\\', '/');

					// Find all materials in the same folder (and subfolders, but we'll filter to exact folder only)
					string[] guids = AssetDatabase.FindAssets("t:Material", new[] { materialFolder });
					if (guids.Length == 0) {
						newMaterials[matIndex] = originalMaterial;
						continue;
					}

					// If strict matching is required, parse the base name pattern
					string nameBase = null;
					if (requiresStrictMatching) {
						string originalName = originalMaterial.name ?? "";
						// Find the last underscore to separate base from suffix
						int lastUnderscore = originalName.LastIndexOf('_');
						// Synty uses _01_A, _01_B etc.; if suffix is just one letter, it's not the significant part
						while (lastUnderscore >= 0 && lastUnderscore < originalName.Length - 1) {
							string suffix = originalName.Substring(lastUnderscore + 1);
							if (suffix.Length != 1 || !char.IsLetter(suffix[0])) {
								break;
							}
							lastUnderscore = lastUnderscore > 0 ? originalName.LastIndexOf('_', lastUnderscore - 1) : -1;
						}
						if (lastUnderscore >= 0 && lastUnderscore < originalName.Length - 1) {
							// Base is everything before and including the underscore
							nameBase = originalName.Substring(0, lastUnderscore + 1);
						}
						if (string.IsNullOrEmpty(nameBase)) {
							// No underscore pattern found, keep original
							newMaterials[matIndex] = originalMaterial;
							continue;
						}
					}

					// Load all materials: same folder, and optionally same base name + number pattern
					List<Material> availableMaterials = new();
					foreach (string guid in guids) {
						string matPath = AssetDatabase.GUIDToAssetPath(guid);
						string matFolder = Path.GetDirectoryName(matPath).Replace('\\', '/');
						if (matFolder != materialFolder) {
							continue;
						}

						Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
						if (mat == null) {
							continue;
						}

						// If strict matching is required, check name pattern
						if (requiresStrictMatching && !string.IsNullOrEmpty(nameBase)) {
							Debug.Log($"testing name base {nameBase} against {mat.name}");
							Debug.Log($"original material: {originalMaterial.name}", originalMaterial);


							string matName = mat.name ?? "";
							// Must start with the same base and have a non-empty suffix after the underscore
							if (!matName.StartsWith(nameBase, StringComparison.OrdinalIgnoreCase)) {
								continue;
							}
							string afterBase = matName.Substring(nameBase.Length);
							if (afterBase.Length == 0) {
								continue;
							}
						}

						availableMaterials.Add(mat);
					}

					if (availableMaterials.Count > 0) {
						Material selectedMaterial = availableMaterials[UnityEngine.Random.Range(0, availableMaterials.Count)];
						newMaterials[matIndex] = selectedMaterial;
					} else {
						newMaterials[matIndex] = originalMaterial;
					}
				}

				// Apply the new materials to the instance renderer
				instanceRenderer.sharedMaterials = newMaterials;
				EditorUtility.SetDirty(instanceRenderer);
			}
		}

		private void OnSceneGUI(SceneView sceneView) {
			if (isScanning || !isStarted || cachedCharacterPrefabs.Count == 0 || cachedAnimationClips.Count == 0) {
				return;
			}

			Event e = Event.current;

			// Capture scene view input while started (so clicking doesn't just select objects).
			if (e.type == EventType.Layout) {
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
				return;
			}

			if (e.type == EventType.MouseDown && e.button == 0 && !e.alt) {
				// Get mouse position in world space and raycast against colliders (e.g. your plane).
				Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

				Vector3 hitPoint, hitNormal;
				bool hasHit = false;

				if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore)) {
					hitPoint = hit.point;
					hitNormal = hit.normal;
					hasHit = true;
				} else {
					// Fallback: world Y=0 plane.
					Plane plane = new Plane(Vector3.up, Vector3.zero);
					if (!plane.Raycast(ray, out float distance)) {
						return;
					}
					hitPoint = ray.GetPoint(distance);
					hitNormal = Vector3.up;
				}

				PlaceCharacterAtPosition(hitPoint, hitNormal, hasHit);
				e.Use();
			}
		}

		private void PlaceCharacterAtPosition(Vector3 position, Vector3 hitNormal, bool hasCollisionHit) {
			List<GameObject> availablePrefabs = GetFilteredCharacterPrefabs();
			List<AnimationClip> availableClips = GetFilteredAnimationClips();

			if (availablePrefabs.Count == 0 || availableClips.Count == 0) {
				Debug.LogWarning("Synty Animation Poser: No characters or animations available (check your Filter Options).");
				return;
			}

			GameObject randomPrefab = availablePrefabs[UnityEngine.Random.Range(0, availablePrefabs.Count)];

			Transform parent = parentTransform != null ? parentTransform : null;
			GameObject instance = PrefabUtility.InstantiatePrefab(randomPrefab) as GameObject;
			if (instance == null) {
				instance = Instantiate(randomPrefab);
			}

			if (parent != null) {
				instance.transform.SetParent(parent);
			}

			Undo.RegisterCreatedObjectUndo(instance, "Place Synty Character");

			AnimationClip randomClip = availableClips[UnityEngine.Random.Range(0, availableClips.Count)];

			// Root motion may move character back to origin, so we pose first and then move afterwards
			Vector3 desiredWorldPos = position;
			ApplyAnimationPose(instance, randomClip);

			Vector3 currentUp = instance.transform.up;
			Vector3 desiredUp = useCollisionNormal && hasCollisionHit ? hitNormal.normalized : alignmentVector.normalized;
			Quaternion alignmentRotation = Quaternion.FromToRotation(currentUp, desiredUp);

			if (randomYRotation) {
				float randomYAngle = UnityEngine.Random.Range(0f, 360f);
				Quaternion yRotation = Quaternion.AngleAxis(randomYAngle, desiredUp);
				alignmentRotation = yRotation * alignmentRotation;
			}

			Quaternion finalRotation = alignmentRotation * instance.transform.rotation;

			instance.transform.SetPositionAndRotation(desiredWorldPos, finalRotation);

			if (rotateHead && (headHorizontalRange > 0f || headVerticalRange > 0f)) {
				ApplyRandomHeadRotation(instance, headHorizontalRange, headVerticalRange);
			}

			if (randomMaterial) {
				ApplyRandomMaterial(instance, randomPrefab);
			}

			Selection.activeGameObject = instance;
		}

		private void ApplyAnimationPose(GameObject character, AnimationClip clip) {
			if (character == null || clip == null) {
				return;
			}

			Animator animator = character.GetComponentInChildren<Animator>();
			if (animator == null) {
				Debug.LogWarning($"Synty Animation Poser: Character '{character.name}' has no Animator component.");
				return;
			}

			GameObject rootObject = animator.gameObject;

			float randomTime = UnityEngine.Random.Range(0f, clip.length);

			try {
				clip.SampleAnimation(rootObject, randomTime);
				EditorUtility.SetDirty(rootObject);
				EditorApplication.QueuePlayerLoopUpdate();
				SceneView.RepaintAll();
			} catch (Exception ex) {
				Debug.LogWarning($"Synty Animation Poser: Failed to sample animation '{clip.name}': {ex.Message}");
			}

			Debug.Log($"Synty Animation Poser: Applied '{clip.name}' at time {randomTime:F2}s to '{character.name}'");
		}

		private void SaveSettings() {
			for (int i = 0; i < animationPacks.items.Count; i++) {
				EditorPrefs.SetBool($"{PREFS_PREFIX}AnimPack_{i}_Enabled", animationPacks.items[i].enabled);
			}
			EditorPrefs.SetInt($"{PREFS_PREFIX}AnimPack_Count", animationPacks.items.Count);

			for (int i = 0; i < artPacks.items.Count; i++) {
				EditorPrefs.SetBool($"{PREFS_PREFIX}ArtPack_{i}_Enabled", artPacks.items[i].enabled);
			}
			EditorPrefs.SetInt($"{PREFS_PREFIX}ArtPack_Count", artPacks.items.Count);

			// Save foldout states
			EditorPrefs.SetBool($"{PREFS_PREFIX}AnimationPacksFoldout", animationPacksFoldout);
			EditorPrefs.SetBool($"{PREFS_PREFIX}ArtPacksFoldout", artPacksFoldout);
			EditorPrefs.SetBool($"{PREFS_PREFIX}PlacementOptionsFoldout", placementOptionsFoldout);
			EditorPrefs.SetBool($"{PREFS_PREFIX}FilterOptionsFoldout", filterOptionsFoldout);

			// Save placement options
			EditorPrefs.SetBool($"{PREFS_PREFIX}UseCollisionNormal", useCollisionNormal);
			EditorPrefs.SetFloat($"{PREFS_PREFIX}AlignmentVector_X", alignmentVector.x);
			EditorPrefs.SetFloat($"{PREFS_PREFIX}AlignmentVector_Y", alignmentVector.y);
			EditorPrefs.SetFloat($"{PREFS_PREFIX}AlignmentVector_Z", alignmentVector.z);
			EditorPrefs.SetBool($"{PREFS_PREFIX}RandomYRotation", randomYRotation);
			EditorPrefs.SetBool($"{PREFS_PREFIX}RandomMaterial", randomMaterial);
			EditorPrefs.SetBool($"{PREFS_PREFIX}RotateHead", rotateHead);
			EditorPrefs.SetFloat($"{PREFS_PREFIX}HeadHorizontalRange", headHorizontalRange);
			EditorPrefs.SetFloat($"{PREFS_PREFIX}HeadVerticalRange", headVerticalRange);

			// Save filter strings
			EditorPrefs.SetString($"{PREFS_PREFIX}AnimationNameFilter", animationNameFilter ?? "");
			EditorPrefs.SetString($"{PREFS_PREFIX}CharacterNameFilter", characterNameFilter ?? "");
		}

		private void LoadSettings() {
			// Load pack enabled states
			int animPackCount = EditorPrefs.GetInt($"{PREFS_PREFIX}AnimPack_Count", -1);
			if (animPackCount > 0 && animPackCount == animationPacks.items.Count) {
				for (int i = 0; i < animPackCount; i++) {
					if (i < animationPacks.items.Count) {
						animationPacks.items[i].enabled = EditorPrefs.GetBool($"{PREFS_PREFIX}AnimPack_{i}_Enabled", animationPacks.items[i].enabled);
					}
				}
			}

			int artPackCount = EditorPrefs.GetInt($"{PREFS_PREFIX}ArtPack_Count", -1);
			if (artPackCount > 0 && artPackCount == artPacks.items.Count) {
				for (int i = 0; i < artPackCount; i++) {
					if (i < artPacks.items.Count) {
						artPacks.items[i].enabled = EditorPrefs.GetBool($"{PREFS_PREFIX}ArtPack_{i}_Enabled", artPacks.items[i].enabled);
					}
				}
			}

			// Load foldout states
			animationPacksFoldout = EditorPrefs.GetBool($"{PREFS_PREFIX}AnimationPacksFoldout", animationPacksFoldout);
			artPacksFoldout = EditorPrefs.GetBool($"{PREFS_PREFIX}ArtPacksFoldout", artPacksFoldout);
			placementOptionsFoldout = EditorPrefs.GetBool($"{PREFS_PREFIX}PlacementOptionsFoldout", placementOptionsFoldout);
			filterOptionsFoldout = EditorPrefs.GetBool($"{PREFS_PREFIX}FilterOptionsFoldout", filterOptionsFoldout);

			// Load placement options
			useCollisionNormal = EditorPrefs.GetBool($"{PREFS_PREFIX}UseCollisionNormal", useCollisionNormal);
			alignmentVector = new Vector3(
				EditorPrefs.GetFloat($"{PREFS_PREFIX}AlignmentVector_X", alignmentVector.x),
				EditorPrefs.GetFloat($"{PREFS_PREFIX}AlignmentVector_Y", alignmentVector.y),
				EditorPrefs.GetFloat($"{PREFS_PREFIX}AlignmentVector_Z", alignmentVector.z)
			);
			randomYRotation = EditorPrefs.GetBool($"{PREFS_PREFIX}RandomYRotation", randomYRotation);
			randomMaterial = EditorPrefs.GetBool($"{PREFS_PREFIX}RandomMaterial", randomMaterial);
			rotateHead = EditorPrefs.GetBool($"{PREFS_PREFIX}RotateHead", rotateHead);
			headHorizontalRange = EditorPrefs.GetFloat($"{PREFS_PREFIX}HeadHorizontalRange", headHorizontalRange);
			headVerticalRange = EditorPrefs.GetFloat($"{PREFS_PREFIX}HeadVerticalRange", headVerticalRange);

			// Load filter strings
			animationNameFilter = EditorPrefs.GetString($"{PREFS_PREFIX}AnimationNameFilter", animationNameFilter ?? "");
			characterNameFilter = EditorPrefs.GetString($"{PREFS_PREFIX}CharacterNameFilter", characterNameFilter ?? "");
		}
	}

}
