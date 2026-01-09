


/*
Register or Injection:

Before MainScene:
    GameData.Core.Collections.DataBaseCore: 
        SpecialGuestGroup(SpawnInfo)
    
    GameData.Core.Collections.CharacterUtility.DataBaseCharacter:
        SpecialGuest
        GuestProfilePair
    
    GameData.Core.Collections.DaySceneUtility.DataBaseLanguage:
        FoodRequest
        BevRequest

    GameData.Core.Collections.DaySceneUtility.DataBaseDay:
        NPC

Before NightScene:
    GameData.CoreLanguage.Collections.NightSceneLanguage:
        evaluation
        conversation

Hook:
    GetPortraitSprite <- DialogPannel.GetSpeakerVisual
    GetPortraitSprite <- SpecialGuestDescriber.Describe

TODO: 
    implement GuestProfilePair totally from ResourceEx, currently only partial implementation.
*/