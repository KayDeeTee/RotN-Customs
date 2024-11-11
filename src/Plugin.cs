using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Shared;
using Shared.PlayerData;
using Shared.SceneLoading;
using Shared.TrackSelection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Http.Headers;
using System.Reflection;
using Shared.MenuOptions;
using Shared.SceneLoading.Payloads;
using Shared.Analytics;
using RhythmRift;
namespace BaseMod;


[BepInPlugin("main.rotn.plugins.base_mod", "Base RotN Mod", "1.0.0.0")]

public class BaseRotnPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    static bool custom_tracks = false;

    static List<CustomTrackMetadata> customTrackMetadataList = new List<CustomTrackMetadata>();

    private void Awake()
    {
        //"CustomTracksMenu"
        //"TrackSelection"

        // Plugin startup logic
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(typeof(BaseRotnPlugin));

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    [HarmonyPatch(typeof(PlayerDataUtil), "GetLockedStatus")]
    [HarmonyPrefix]
    public static bool GetLockedStatus(ref bool __result){
        __result = false;
        return false;
    }

    public static void set_var_by_name( object instance, string name, object value ){
        Type t = instance.GetType();
        FieldInfo property = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null){
            property.SetValue(instance, Convert.ChangeType(value, property.FieldType));
        } else {
            property = t.BaseType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(instance, Convert.ChangeType(value, property.FieldType));
        }
    }

    public static object get_var_by_name( object instance, string name ){
        FieldInfo property = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return property.GetValue(instance);
    }

    [HarmonyPatch(typeof(RRStageController), "UnpackScenePayload")]
    [HarmonyPrefix]
    public static void CustomUnpackState(ScenePayload __0, out ScenePayload __state){
        __state = __0;
    }

    [HarmonyPatch(typeof(RRStageController), "UnpackScenePayload")]
    [HarmonyFinalizer]
    public static void CustomUnpack(Exception __exception, ScenePayload __state, ref RRStageController __instance,
        ref RRStageUIView ____stageUIView, ref GameObject ____practiceModeTextObject,
        ref RRBeatmapPlayer ____beatmapPlayer, ref AccuracyBar ____accuracyBar
    ){

        if ( !custom_tracks ){
            return;
        }
        RRCustomTrackScenePayload rrcustomTrackScenePayload = __state as RRCustomTrackScenePayload;

        set_var_by_name(__instance, "_isPracticeMode", rrcustomTrackScenePayload.IsPracticeMode);
		set_var_by_name(__instance, "_shouldPracticeModeLoop", rrcustomTrackScenePayload.ShouldPracticeModeLoop && rrcustomTrackScenePayload.IsPracticeMode);
		set_var_by_name(__instance, "_shouldShowEarlyLateHits", PlayerSaveController.Instance.CurrentSaveData.ShouldShowEarlyLateHits || rrcustomTrackScenePayload.IsPracticeMode);
        set_var_by_name(__instance, "_shouldShowAccuracyBar", PlayerSaveController.Instance.CurrentSaveData.ShouldShowAccuracyBar || rrcustomTrackScenePayload.IsPracticeMode);
        set_var_by_name(__instance, "_shouldHideGuitarStrings", PlayerSaveController.Instance.CurrentSaveData.ShouldDisableGuitarStrings);
        set_var_by_name(__instance, "_shouldShowReducedComboScoreVFX", PlayerSaveController.Instance.CurrentSaveData.ShouldShowReducedComboScoreVFX);
        set_var_by_name(__instance, "_shouldShowSimpleBackground", PlayerSaveController.Instance.CurrentSaveData.ShouldShowSimpleBackground);

		if (rrcustomTrackScenePayload.IsPracticeMode)
		{
			____practiceModeTextObject.SetActive(true);
            set_var_by_name(__instance, "_practiceModeTotalStageBeats", rrcustomTrackScenePayload.TotalBeats);
			bool changed = false;
			float practiceStart = rrcustomTrackScenePayload.PracticeModeStartBeat;
			float practiceEnd = rrcustomTrackScenePayload.PracticeModeEndBeat;
            Logger.LogInfo($"Practice mode: from {practiceStart} to {practiceEnd}");
            float pstart2 = practiceStart;
            float pend2 = practiceEnd;
			if (practiceStart - 8f < 0f) {
                set_var_by_name(__instance, "_practiceModeStartBeatNumber", 0f);
                pstart2 = 0f;
				changed = true;
			}
			else if (practiceStart - 8f > rrcustomTrackScenePayload.TotalBeats - 1f) {
                set_var_by_name(__instance, "_practiceModeTotalStageBeats", rrcustomTrackScenePayload.TotalBeats - 8f - 1f);
				changed = true;
			}
			else {
                set_var_by_name(__instance, "_practiceModeStartBeatNumber", practiceStart - 8f);
                pstart2 = practiceStart - 8f;
			}
			if (practiceEnd <= 0f || practiceEnd > rrcustomTrackScenePayload.TotalBeats) {
                set_var_by_name(__instance, "_practiceModeEndBeatNumber", rrcustomTrackScenePayload.TotalBeats);
                pend2 = rrcustomTrackScenePayload.TotalBeats;
				changed = true;
			}
			else if (practiceEnd <= pstart2)
			{
                set_var_by_name(__instance, "_practiceModeEndBeatNumber", pstart2 + 1f);
                pend2 = pstart2 + 1f;
				changed = true;
			}
			else {
                 set_var_by_name(__instance, "_practiceModeEndBeatNumber", practiceEnd);
                 pend2 = practiceEnd;
			}
			if (changed)
			{
				rrcustomTrackScenePayload.SetPracticeModeBeatRange(pstart2, pend2);
			}
			float first_beat = pstart2;
            set_var_by_name(__instance, "_practiceModeStartBeatmapIndex", 0);
            set_var_by_name(__instance, "_practiceModeTotalBeatsSkippedBeforeStartBeatmap", 0f);
		}

		____stageUIView.SetBeatmapProgressBarDisplayStatus(true);
		____stageUIView.SetBeatmapProgressBarTrueBeatNumberStatus(true);
		____stageUIView.SetCalibrationTest(false);
		if (rrcustomTrackScenePayload.IsPracticeMode) {	____stageUIView.SetPracticeModeTextStatus(true); }
		____stageUIView.ToggleReducedVFX(PlayerSaveController.Instance.CurrentSaveData.ShouldShowReducedComboScoreVFX);
		____stageUIView.ToggleSimpleBackground(PlayerSaveController.Instance.CurrentSaveData.ShouldShowSimpleBackground);

        if (____accuracyBar)
	    {
		    ____accuracyBar.Initialize( (int)get_var_by_name(__instance, "_totalInputsToShowInAccuracyBar"), ____beatmapPlayer.ActiveInputRatingsDefinition);
	    }
    }

    [HarmonyPatch(typeof(SceneLoadingController), "GoToScene", new Type[] { typeof(string), typeof(Action), typeof(bool)})]
    [HarmonyPrefix]
    public static void GoToScene(ref string __0){
        if( __0 == "TrackSelection" ){
            custom_tracks = false;
        }
        if( __0 == "CustomTracksMenu" ){
            custom_tracks = true;
            __0 = "TrackSelection";
        }
    }

    [HarmonyPatch(typeof(InfiniteTrackSelectionOption), "SetInfo")]
    [HarmonyPrefix]
    public static bool InfSetInfo(ref string __0, ref bool __runOriginal,
        ref string ____levelId, ref bool ____isLocked, ref TMP_Text ____trackNameText, ref TMP_Text ____artistNameText,
        ref Image ____albumArtImage, ref GameObject ____lockedOverlay, ref GameObject ____letterGradeParent, ref bool ____hasInfoBeenSet
    ){
        if( custom_tracks ){
            CustomTrackMetadata metadata = new CustomTrackMetadata();
            foreach( CustomTrackMetadata custom in customTrackMetadataList ){
                if( custom.LevelId == __0 ) metadata = custom;
            }
            if( metadata.LevelId == __0 ) { //found match
                ____levelId = __0;
                ____isLocked = false;
                ____trackNameText.text = metadata.TrackName;
                ____trackNameText.ForceMeshUpdate(false, false);
                ____trackNameText.verticalAlignment = VerticalAlignmentOptions.Middle;

                ____artistNameText.text = metadata.ArtistName;
                ____artistNameText.ForceMeshUpdate(false, false);
                ____artistNameText.verticalAlignment = VerticalAlignmentOptions.Middle;
                
                ____albumArtImage.sprite = metadata.AlbumArtSprite;

                ____lockedOverlay.SetActive(false);
                ____letterGradeParent.SetActive( true );
                ____hasInfoBeenSet = true;
            }

            __runOriginal = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(InfiniteTrackSelectionOptionGroup), "RefreshTrackOptions")]
    [HarmonyPrefix]
    public static bool InfRefresh(ref InfiniteTrackSelectionOptionGroup __instance, ref bool __runOriginal,
        int ____selectionIndex, List<InfiniteTrackSelectionOption> ____options,
        int ____selectedTrackIndex, List<RRTrackMetaData> ____trackMetaData,
        int ____numActiveTrackOptions
    ){
        if( custom_tracks ){
            if( !__instance.IsInitialized ){
                return true;
            }
            MethodInfo get_bound_index = typeof(InfiniteTrackSelectionOptionGroup).GetMethod("GetBoundIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            
            RRTrackMetaData current_track = ____trackMetaData[____selectedTrackIndex];
            InfiniteTrackSelectionOption current_sel = ____options[____selectionIndex];
            CustomTrackMetadata ct = get_meta_from_id( current_track.LevelId );
            current_sel.SetInfo( ct.LevelId, ct.TrackName, ct.ArtistName, ct.AlbumArtSprite, "", false, 0, false);

            Logger.LogMessage( "Refreshed Selected" );
            int below_sel = Mathf.CeilToInt((float)(____numActiveTrackOptions - 1) / 2f);
            for (int i = 1; i <= below_sel; i++)
			{
                int bound_track_idx = (int) get_bound_index.Invoke( __instance, new object[]{ ____selectedTrackIndex + i, ____trackMetaData.Count } );
                int bound_option_idx = (int) get_bound_index.Invoke( __instance, new object[]{ ____selectionIndex + i, ____options.Count } );

                RRTrackMetaData below_track = ____trackMetaData[bound_track_idx];
                InfiniteTrackSelectionOption below_option = ____options[bound_option_idx];
                CustomTrackMetadata below_ct = get_meta_from_id( below_track.LevelId );
                below_option.SetInfo( below_ct.LevelId, below_ct.TrackName, below_ct.ArtistName, below_ct.AlbumArtSprite, "", false, 0, false);
                Logger.LogMessage( $"Refreshed Below {i}" );
            }

            int above_sel = Mathf.FloorToInt((float)(____numActiveTrackOptions - 1) / 2f);
            for (int i = 1; i <= above_sel; i++)
			{
                int bound_track_idx = (int) get_bound_index.Invoke( __instance, new object[]{ ____selectedTrackIndex - i, ____trackMetaData.Count } );
                int bound_option_idx = (int) get_bound_index.Invoke( __instance, new object[]{ ____selectionIndex - i, ____options.Count } );

                RRTrackMetaData below_track = ____trackMetaData[bound_track_idx];
                InfiniteTrackSelectionOption below_option = ____options[bound_option_idx];
                CustomTrackMetadata below_ct = get_meta_from_id( below_track.LevelId );
                below_option.SetInfo( below_ct.LevelId, below_ct.TrackName, below_ct.ArtistName, below_ct.AlbumArtSprite, "", false, 0, false);
                Logger.LogMessage( $"Refreshed Above {i}" );
            }
            
            MethodInfo update_positions = typeof(InfiniteTrackSelectionOptionGroup).GetMethod("UpdateTrackOptionPositions", BindingFlags.NonPublic | BindingFlags.Instance);
            update_positions.Invoke(__instance, new object[]{});
            __runOriginal = false;
            return false;
        }
        return true;
    }

    public static CustomTrackMetadata get_meta_from_id( string level_id ){
        foreach( CustomTrackMetadata ct in customTrackMetadataList ){
            if( ct.LevelId == level_id ) return ct;
        }
        Logger.LogError($"{level_id} : not found!!!" );
        return new CustomTrackMetadata();
    }

    //[HarmonyPatch(typeof(TrackSelectionSceneController), "FillInTrackDetails")]
    //[HarmonyPrefix]
    //public static void TSSC_FillInTrackDetails( ref bool __0, ref int ____selectedTrackIndex, ref TrackSelectionSceneController __instance, bool __runOriginal ){
    //    if( custom_tracks ){
    //        __runOriginal = false;
    //        Logger.LogInfo($"Filling in data for : {____selectedTrackIndex}");
    //    }
    //}
    
    [HarmonyPatch(typeof(TrackSelectionSceneController), "HandleContinueToStage")]
    [HarmonyPrefix]
    public static bool TSSC_HandleStage( ref bool __runOriginal, 
        ref TrackSelectionSceneController __instance,
        ref OptionsScreenInputController ____optionsInputController, ref bool ____isPerformingAction,
        int ____selectedTrackIndex, RRTrackMetaData[] ____trackMetaDatas, Difficulty ____selectedDifficulty,
        bool ____isPracticeMode

    ){
        if( custom_tracks ){

			____optionsInputController.IsInputDisabled = true;
			____isPerformingAction = true;
			//if (this._startTrackSfx.Guid != Guid.Empty)
			//{
			//	AudioManager.Instance.PlayAudioEvent(this._startTrackSfx, 0f, false, 0U, 0f, false);
			//}
            RRTrackMetaData rrtrackMetaData = ____trackMetaDatas[____selectedTrackIndex];
			CustomTrackMetadata ct = get_meta_from_id( rrtrackMetaData.LevelId );
			RRCustomTrackScenePayload rrcustomTrackScenePayload = new RRCustomTrackScenePayload();
			rrcustomTrackScenePayload.SetDestinationScene("RhythmRift");
			rrcustomTrackScenePayload.InitializeFromMetadata(ct, ____selectedDifficulty);
			rrcustomTrackScenePayload.IsPracticeMode = ____isPracticeMode;
			rrcustomTrackScenePayload.ShouldPracticeModeLoop = true;
			SceneLoadData.SetCurrentScenePayload(rrcustomTrackScenePayload);
            MethodInfo get_ret = typeof( TrackSelectionSceneController).GetMethod("CreateReturnScenePayload", BindingFlags.NonPublic | BindingFlags.Instance);
			TrackSelectionScenePayload return_payload = (TrackSelectionScenePayload)get_ret.Invoke(__instance, new object[]{ ct.LevelId });
            return_payload.SetDestinationScene( "CustomTracksMenu" );
            SceneLoadData.SetReturnScenePayload( return_payload );
			ScenePayload scenePayload;
			if (SceneLoadData.TryGetCurrentPayload(out scenePayload))
			{
				SceneLoadData.StageEntryType = RiftAnalyticsService.StageEntryType.StageSelectMenu;
				SceneLoadingController.Instance.GoToScene(scenePayload.GetDestinationScene(), null, true);
			}

            __runOriginal = false;
            return false;
            
        }
        return true;

    }

    [HarmonyPatch(typeof(TrackSelectionSceneController), "HandleTrackSubmitted")]
    [HarmonyPrefix]
    public static bool TSSC_HandleSubmit( ref string __0, ref bool __runOriginal, 
        ref LoadoutScreenManager ____loadoutScreen, ref OptionsScreenInputController ____optionsInputController, ref bool ____isPerformingAction
    ){
        if( custom_tracks ){
            ____optionsInputController.IsInputDisabled = true;
            ____isPerformingAction = true;

            CustomTrackMetadata ct = get_meta_from_id( __0 );
            ____loadoutScreen.Show(Difficulty.Impossible, ct.TrackName, ct.AlbumArtSprite, ct.DifficultyInformation[3].Intensity, false, ct.BeatCount, false);

            __runOriginal = false;
            return false;
        }
        return true;
    }


    [HarmonyPatch(typeof(TrackSelectionSceneController), "GetTrackMetadataFromDatabase")]
    [HarmonyPrefix]
    public static void TSSC_GetTrackMetadataFromDatabase(ref RRTrackMetaData[] ____trackMetaDatas, ref bool __runOriginal){
        if( custom_tracks ){
            customTrackMetadataList = new List<CustomTrackMetadata>();
            string path = Application.persistentDataPath + Path.DirectorySeparatorChar.ToString() + "CustomTracks";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (string custom_track in Directory.GetDirectories(path)){
                Logger.LogInfo($"Loading Song: {custom_track}");
                string metadata_json = custom_track + Path.DirectorySeparatorChar.ToString() + "info.json";
                string[] path_split = custom_track.Split(Path.DirectorySeparatorChar, StringSplitOptions.None);
                string levelId = path_split[path_split.Length - 1];
                CustomTrackMetadata ct_metadata = JsonUtility.FromJson<CustomTrackMetadata>(File.ReadAllText(metadata_json));
                ct_metadata.LevelId = levelId;
                for (int i = 0; i < ct_metadata.DifficultyInformation.Length; i++){
                    CustomTrackDifficultyInformation ct_diff_info = ct_metadata.DifficultyInformation[i];
                    Difficulty difficulty;
                    if( Enum.TryParse<Difficulty>( ct_diff_info.DifficultyLabel, out difficulty) ){
                        ct_diff_info.Difficulty = difficulty;
                        ct_metadata.DifficultyInformation[i] = ct_diff_info;
                    }
                }
                if( ct_metadata.DifficultyInformation.Length != 0 ){
                    string album_art = custom_track + Path.DirectorySeparatorChar.ToString() + ct_metadata.AlbumArtFileName;
                    if( File.Exists(album_art) ){
                        Texture2D album_texture = new Texture2D(2,2);
                        album_texture.LoadImage( File.ReadAllBytes(album_art) );
                        ct_metadata.AlbumArtSprite = Sprite.Create(album_texture, new Rect(new Vector2(0f, 0f), new Vector2((float)album_texture.width, (float)album_texture.height)), new Vector2(0.5f, 0.5f));
                    }
                    customTrackMetadataList.Add(ct_metadata);
                    Logger.LogInfo($"Loaded Song: {custom_track}");
                }

                
            }
            ____trackMetaDatas = [];

            List<RRTrackMetaData> track_list = new List<RRTrackMetaData>();
            foreach( CustomTrackMetadata custom in customTrackMetadataList ){
                RRTrackMetaData rr_meta = new RRTrackMetaData();
                rr_meta.LevelId = custom.LevelId;
                rr_meta.TrackPreviewBPM = (int)custom.BeatsPerMinute;
                rr_meta.ShouldHideLeaderboard = true;
                rr_meta.IsFiller = false;
                rr_meta.IsTutorial = false;
                rr_meta.IsLocked = false;

                track_list.Add( rr_meta );
                
            }
            ____trackMetaDatas = track_list.ToArray();

            __runOriginal = false;
        }        
    }
}
