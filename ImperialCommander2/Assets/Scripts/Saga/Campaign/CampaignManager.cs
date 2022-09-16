using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Saga
{
	public class CampaignManager : MonoBehaviour
	{
		[HideInInspector]
		public SagaCampaign sagaCampaign;

		public Fader fader;
		public VolumeProfile volume;
		public GameObject leftPanel, rightPanel, switchButton;
		//prefabs
		public GameObject forceMissionItemPrefab, listItemPrefab, missionItemPrefab;
		//UI
		public AddItemHeroAllyVillainPopup addItemHeroAllyVillainPopup;
		public TMP_InputField campaignNameInputField;
		public Transform villainContainer, allyContainer, itemContainer, structureContainer;
		public CampaignHeroPrefab[] heroPrefabs;
		public TextMeshProUGUI xpText, creditsText, fameText, awardsText;
		public MWheelHandler xpWheel, creditsWheel, fameWheel, awardsWheel;
		public Text campaignExpansion;

		Sound sound;
		int view = 1;//0=left, 1=right

		void Start()
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			Screen.sleepTimeout = SleepTimeout.NeverSleep;

			float pixelHeightOfCurrentScreen = Screen.height;//.currentResolution.height;
			float pixelWidthOfCurrentScreen = Screen.width;//.currentResolution.width;
			float aspect = pixelWidthOfCurrentScreen / pixelHeightOfCurrentScreen;
			if ( aspect < 1.7f )//less than 16:9, such as 16:10 and 4:3
			{
				//switch to single view
				switchButton.SetActive( true );
				leftPanel.SetActive( false );
			}

			//DEBUG BOOTSTRAP A CAMPAIGN
			bootstrapDEBUG();

			fader.UnFade( 2 );

			//apply settings
			sound = FindObjectOfType<Sound>();
			sound.CheckAudio();
			//set translated UI
			SetTranslatedUI();

			if ( volume.TryGet<Bloom>( out var bloom ) )
				bloom.active = PlayerPrefs.GetInt( "bloom" ) == 1;
			if ( volume.TryGet<Vignette>( out var vig ) )
				vig.active = PlayerPrefs.GetInt( "vignette" ) == 1;

			//popuplate UI with loaded campaign data
			InitUI();
		}

		void bootstrapDEBUG()
		{
			Debug.Log( "***BOOTSTRAP DEBUG***" );
			DataStore.InitData();
			//campaign is already setup from Title screen
			if ( RunningCampaign.sagaCampaignGUID != null && RunningCampaign.sagaCampaignGUID != Guid.Empty )
			{
				sagaCampaign = SagaCampaign.LoadCampaignState( RunningCampaign.sagaCampaignGUID );
			}
			else//setup new test campaign
			{
				RunningCampaign.expansionCode = "Core";
				sagaCampaign = SagaCampaign.CreateNewCampaign( "Default", RunningCampaign.expansionCode );
			}
		}

		/// <summary>
		/// fill in name and item/villain/ally lists from campaign data
		/// </summary>
		private void InitUI()
		{
			campaignNameInputField.text = sagaCampaign.campaignName;
			//use translated expansion name
			campaignExpansion.text = DataStore.translatedExpansionNames[sagaCampaign.campaignExpansionCode];
			creditsWheel.ResetWheeler( sagaCampaign.credits );
			xpWheel.ResetWheeler( sagaCampaign.XP );
			fameWheel.ResetWheeler( sagaCampaign.fame );
			awardsWheel.ResetWheeler( sagaCampaign.awards );

			//items
			foreach ( var item in sagaCampaign.campaignItems )
				AddItemToUI( sagaCampaign.GetItemFromID( item ) );
			//allies
			foreach ( var id in sagaCampaign.campaignAllies )
				AddAllyToUI( DataStore.GetAlly( id ) );
			foreach ( var id in sagaCampaign.campaignVillains )
				AddVillainToUI( DataStore.GetEnemy( id ) );
			//heroes
			int c = sagaCampaign.campaignHeroes.Count;
			for ( int i = 0; i < c; i++ )
				heroPrefabs[i].AddHeroToUI( sagaCampaign.campaignHeroes[i] );
			//campaign structure
			foreach ( Transform item in structureContainer )
				Destroy( item.gameObject );
			foreach ( var item in sagaCampaign.campaignStructure )
			{
				var go = Instantiate( missionItemPrefab, structureContainer );
				go.GetComponent<MissionItemPrefab>().Init( item );
			}
			//add 1 force mission item
			var fgo = Instantiate( forceMissionItemPrefab, structureContainer );
			fgo.GetComponent<ForceMissionItemPrefab>().Init( OnAddForcedMissionClick );
		}

		void SetTranslatedUI()
		{
			UICampaign ui = DataStore.uiLanguage.uiCampaign;

		}

		#region CAMPAIGN UI
		void AddAllyToUI( DeploymentCard a )
		{
			var go = Instantiate( listItemPrefab, allyContainer );
			go.GetComponent<CampaignListItemPrefab>().InitAlly( a.name, ( n ) =>
			{
				sagaCampaign.campaignAllies.Remove( a.id );
				Destroy( go );
			} );
		}

		void AddVillainToUI( DeploymentCard v )
		{
			var go = Instantiate( listItemPrefab, villainContainer );
			go.GetComponent<CampaignListItemPrefab>().InitVillain( v.name, ( n ) =>
			{
				sagaCampaign.campaignVillains.Remove( v.id );
				Destroy( go );
			} );
		}

		void AddItemToUI( CampaignItem item )
		{
			var go = Instantiate( listItemPrefab, itemContainer );
			go.GetComponent<CampaignListItemPrefab>().InitGeneralItem( item.name, ( n ) =>
			{
				sagaCampaign.campaignItems.Remove( item.id );
				Destroy( go );
			} );
		}

		public void OnAddAlly()
		{
			addItemHeroAllyVillainPopup.AddAlly( ( a ) =>
			{
				if ( !sagaCampaign.campaignAllies.Contains( a.id ) )
				{
					sagaCampaign.campaignAllies.Add( a.id );
					AddAllyToUI( a );
				}
			} );
		}

		public void OnAddVillain()
		{
			addItemHeroAllyVillainPopup.AddVillain( ( v ) =>
			{
				if ( !sagaCampaign.campaignVillains.Contains( v.id ) )
				{
					sagaCampaign.campaignVillains.Add( v.id );
					AddVillainToUI( v );
				}
			} );
		}

		public void OnAddItem()
		{
			addItemHeroAllyVillainPopup.AddItem( ( item ) =>
			{
				if ( !sagaCampaign.campaignItems.Contains( item.id ) )
				{
					sagaCampaign.campaignItems.Add( item.id );
					AddItemToUI( item );
				}
			} );
		}

		public void OnXPChanged()
		{
			sagaCampaign.XP = xpWheel.wheelValue;
		}

		public void OnCreditsChanged()
		{
			sagaCampaign.credits = creditsWheel.wheelValue;
		}

		public void OnFameChanged()
		{
			sagaCampaign.fame = fameWheel.wheelValue;
		}

		public void OnAwardsChanged()
		{
			sagaCampaign.awards = awardsWheel.wheelValue;
		}
		#endregion

		public void AddHeroToCampaign( CampaignHero hero )
		{
			sagaCampaign.campaignHeroes.Add( hero );
		}

		public void RemoveHeroFromCampaign( CampaignHero hero )
		{
			sagaCampaign.campaignHeroes.Remove( hero );
		}

		public void OnAddForcedMissionClick()
		{
			addItemHeroAllyVillainPopup.AddForcedMission( sagaCampaign.campaignExpansionCode, AddForcedMission );
		}

		public void OnMissionNameClick( MissionType missionType, Action<MissionCard> callback )
		{
			addItemHeroAllyVillainPopup.AddMission( sagaCampaign.campaignExpansionCode, missionType, callback );
		}

		void AddForcedMission( MissionCard card )
		{
			var cs = new CampaignStructure()
			{
				missionType = MissionType.Forced,
				missionID = card.id,
				threatLevel = SagaCampaign.GetMissionPreset( card.expansion, card.id ).defaultThreat,
				isForced = true,
				expansionCode = card.expansion.ToString()
			};
			//remove the "add forced mission" prefab
			foreach ( Transform item in structureContainer )
			{
				if ( item.GetComponent<ForceMissionItemPrefab>() != null )
					Destroy( item.gameObject );
			}
			//add the newly added mission
			var go = Instantiate( missionItemPrefab, structureContainer );
			go.GetComponent<MissionItemPrefab>().Init( cs );
			//add the "forced mission" prefab back to bottom of list
			var fgo = Instantiate( forceMissionItemPrefab, structureContainer );
			fgo.GetComponent<ForceMissionItemPrefab>().Init( OnAddForcedMissionClick );
		}

		public void RemoveForcedMission( string missionID )
		{
			foreach ( Transform item in structureContainer )
			{
				var pf = item.GetComponent<MissionItemPrefab>();
				if ( pf != null )
				{
					if ( pf.campaignStructure.missionID.ToLower() == missionID.ToLower() )
						Destroy( item.gameObject );
				}
			}
		}

		public void OnSaveCampaign()
		{
			var list = new List<CampaignStructure>();
			foreach ( Transform item in structureContainer )
			{
				var cs = item.GetComponent<MissionItemPrefab>();
				if ( cs != null )
					list.Add( cs.campaignStructure );
			}

			sagaCampaign.SaveCampaignState( list );
		}

		public void OnInfoClicked()
		{
			GlowEngine.FindUnityObject<CampaignMessagePopup>().Show( "campaign setup", Utils.ReplaceGlyphs( sagaCampaign.GetCampaignInfo() ), 700 );
		}

		public void OnSwitchViewClicked()
		{
			view = view == 0 ? 1 : 0;
			leftPanel.SetActive( view == 0 );
			rightPanel.SetActive( view == 1 );
		}

		public void OnExitCampaignScreen()
		{
			fader.Fade();
			float foo = 1;
			DOTween.To( () => foo, x => foo = x, 0, .5f ).OnComplete( () =>
			 SceneManager.LoadScene( "Title" ) );
		}

		public void OnEndEditCampaignName()
		{
			sagaCampaign.campaignName = campaignNameInputField.text;
		}
	}
}