using System;
using BepInEx.Logging;
using UnityEngine;
using Common.CharacterUtility;
using DayScene.Interactables.Collections.ConditionComponents;

namespace MetaMystia;

public class KyoukoManager
{
    private static KyoukoManager _instance;
    private static readonly object _lock = new object();
    
    private const string KYOUKO_ID = "Kyouko";
    private static ManualLogSource Log => Plugin.Instance.Log;

    public static KyoukoManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new KyoukoManager();
                    }
                }
            }
            return _instance;
        }
    }

    private KyoukoManager()
    {
    }

    public CharacterConditionComponent GetCharacterComponent()
    {
        var characters = DayScene.DaySceneMap.allCharacters;
        if (characters == null)
        {
            Log.LogMessage("allCharacters 为空");
            return null;
        }

        if (!characters.ContainsKey(KYOUKO_ID))
        {
            Log.LogMessage($"未找到 ID 为 '{KYOUKO_ID}' 的角色");
            return null;
        }

        var component = characters[KYOUKO_ID];
        if (component == null)
        {
            Log.LogMessage($"角色 '{KYOUKO_ID}' 的 CharacterConditionComponent 为空");
            return null;
        }

        return component;
    }

    public CharacterControllerUnit GetCharacterUnit()
    {
        var component = GetCharacterComponent();
        if (component == null)
        {
            return null;
        }

        var characterUnit = component.Character;
        if (characterUnit == null)
        {
            Log.LogMessage($"角色 '{KYOUKO_ID}' 的 CharacterControllerUnit 为空");
            return null;
        }

        return characterUnit;
    }

    public Rigidbody2D GetRigidbody2D()
    {
        var characterUnit = GetCharacterUnit();
        if (characterUnit == null)
        {
            return null;
        }

        var rb = characterUnit.rb2d;
        if (rb == null)
        {
            Log.LogMessage($"角色 '{KYOUKO_ID}' 的 Rigidbody2D 为空");
            return null;
        }

        return rb;
    }

    public Vector2? GetPosition()
    {
        var rb = GetRigidbody2D();
        return rb?.position;
    }

    public bool SetPosition(float x, float y)
    {
        var rb = GetRigidbody2D();
        if (rb == null)
        {
            return false;
        }
        rb.position = new Vector2(x, y);
        Log.LogMessage($"已设置 Kyouko 位置到 ({x}, {y})");
        return true;
    }

    public bool? GetMoving()
    {
        var characterUnit = GetCharacterUnit();
        if (characterUnit == null)
        {
            return null;
        }
        return characterUnit.IsMoving;
    }

    public bool SetMoving(bool isMoving)
    {
        var characterUnit = GetCharacterUnit();
        if (characterUnit == null)
        {
            return false;
        }
        characterUnit.IsMoving = isMoving;
        Log.LogMessage($"已设置 Kyouko 移动状态为 {isMoving}");
        return true;
    }

    public GameObject GetGameObject()
    {
        var component = GetCharacterComponent();
        return component?.gameObject;
    }
}
