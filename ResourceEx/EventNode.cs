using UnityEngine;
using System.Linq;

using GameData.Profile;
using GameData.Profile.SchedulerNodeCollection;
using GameData.Core.Collections;
using GameData.CoreLanguage;
using GameData.CoreLanguage.Collections;

using static GameData.Core.Collections.Sellable;
using static GameData.Profile.SchedulerNodeCollection.MissionNode;
using static GameData.Core.Collections.DaySceneUtility.Collections.Product;
using static GameData.Profile.SchedulerNode.Trigger;

using MetaMystia.ResourceEx.Models;
using DEYU.Utils;

namespace MetaMystia;


public static partial class ResourceExManager
{
    private static void ActivateAllKizunaEventNodes()
    {
        _characterConfigs.Values
            .Where(c => c.kizuna != null)
            .ToList()
            .ForEach(CheckAndActivateKizunaEventNode);
        Log.Info("Kizuna event nodes activation completed.");
    }

    private static void CheckAndActivateKizunaEventNode(CharacterConfig config)
    {
        var scheduledEvents = GameData.RunTime.Common.RunTimeScheduler.scheduledEvents;
        if (scheduledEvents[-1].Contains(config.kizuna.lv1UpgradePrerequisiteEvent) ||
            scheduledEvents[-1].Contains(config.kizuna.lv2UpgradePrerequisiteEvent) ||
            scheduledEvents[-1].Contains(config.kizuna.lv3UpgradePrerequisiteEvent) ||
            scheduledEvents[-1].Contains(config.kizuna.lv4UpgradePrerequisiteEvent))
        {
            return;
        }

        var currentBondLevel = GameData.RunTime.Common.RunTimeAlbum.RefOrGenerateSpecialRunTimeData(config.id).CurrentBondLevel;
        switch (currentBondLevel)
        {
            case 1:
                scheduledEvents[-1].Add(config.kizuna.lv1UpgradePrerequisiteEvent);
                break;
            case 2:
                scheduledEvents[-1].Add(config.kizuna.lv2UpgradePrerequisiteEvent);
                break;
            case 3:
                scheduledEvents[-1].Add(config.kizuna.lv3UpgradePrerequisiteEvent);
                break;
            case 4:
                scheduledEvents[-1].Add(config.kizuna.lv4UpgradePrerequisiteEvent);
                break;
            case 5:
                break;
            default:
                Log.Error($"Invalid bond level {currentBondLevel} for character {config.name}({config.id})");
                break;
        }
        Log.Info($"Activated kizuna event node for character {config.name}({config.id}) at bond level {currentBondLevel}");
    }
    private static void RegisterAllEventNodes() => EventNodeConfigs.ToList().ForEach(RegisterEventNode);
    private static void RegisterEventNode(EventNodeConfig config)
    {
        var eventNode = ScriptableObject.CreateInstance<EventNode>();
        eventNode.name = config.label;
        eventNode.label = config.label;
        eventNode.debugLabel = config.debugLabel;
        eventNode.scheduledEvent = new SchedulerNode.ScheduledEvent()
        {
            trigger = BuildEventTrigger(config.trigger, config.debugLabel)
        };

        if (config.rewards != null)
        {
            eventNode.rewards = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<MissionNode.Reward>(config.rewards.Count);
            for (int i = 0; i < config.rewards.Count; i++)
            {
                eventNode.rewards[i] = BuildMissionReward(config.rewards[i]);
            }
        }
        else
        {
            eventNode.rewards = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<MissionNode.Reward>(0);
        }

        eventNode.postMissionsAfterPerformance = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray(config.postMissionsAfterPerformance.Count);
        for (int i = 0; i < config.postMissionsAfterPerformance.Count; i++)
        {
            eventNode.postMissionsAfterPerformance[i] = config.postMissionsAfterPerformance[i];
        }

        eventNode.postEvents = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray(config.postEvents.Count);
        for (int i = 0; i < config.postEvents.Count; i++)
        {
            eventNode.postEvents[i] = config.postEvents[i];
        }

        var _ = DataBaseScheduler.allNodes.TryAdd(config.label, eventNode);
        Log.Info($"Registering EventNode {config.debugLabel}({config.label}), success: {_}");
    }

    private static SchedulerNode.Trigger BuildEventTrigger(TriggerConfig config, string debugLabel)
    {
        var trigger = new SchedulerNode.Trigger();
        if (config == null) return trigger;

        trigger.triggerType = config.triggerType;
        switch (config.triggerType)
        {
            case TriggerType.KizunaCheckPoint:
                trigger.triggerId = config.triggerId; // TODO: make sure triggerId in allCharacters
                break;
            default:
                Log.Error($"Unsupported event trigger type {config.triggerType} for event {debugLabel}");
                break;
        }
        return trigger;
    }
}
