﻿using Barotrauma.Networking;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Voronoi2;

namespace Barotrauma
{
    partial class Character
    {
        public enum EventType
        {
            InventoryState = 0,
            Control = 1,
            Status = 2,
            Treatment = 3,
            SetAttackTarget = 4,
            ExecuteAttack = 5,
            AssignCampaignInteraction = 6,
            ObjectiveManagerState = 7,
            TeamChange = 8,
            AddToCrew = 9,
            UpdateExperience = 10,
            UpdateTalents = 11,
            UpdateSkills = 12,
            UpdateMoney = 13,
            UpdatePermanentStats = 14,
            RemoveFromCrew = 15,
            LatchOntoTarget = 16,
            UpdateTalentRefundPoints = 17,
            ConfirmTalentRefund = 18,

            MinValue = 0,
            MaxValue = 18
        }

        private interface IEventData : NetEntityEvent.IData
        {
            public EventType EventType { get; }
        }

        public readonly struct InventoryStateEventData : IEventData
        {
            public EventType EventType => EventType.InventoryState;
            public readonly Range SlotRange;

            public InventoryStateEventData(Range slotRange)
            {
                SlotRange = slotRange;
            }
        }
        
        public readonly struct ControlEventData : IEventData
        {
            public EventType EventType => EventType.Control;
            public readonly Client Owner;
            
            public ControlEventData(Client owner)
            {
                Owner = owner;
            }
        }

        public struct CharacterStatusEventData : IEventData
        {
            public EventType EventType => EventType.Status;

#if SERVER
            public bool ForceAfflictionData;

            public CharacterStatusEventData(bool forceAfflictionData)
            {
                ForceAfflictionData = forceAfflictionData;
            }
#endif
        }

        public struct TreatmentEventData : IEventData
        {
            public EventType EventType => EventType.Treatment;
        }

        private interface IAttackEventData : IEventData
        {
            public Limb AttackLimb { get; }
            public IDamageable TargetEntity { get; }
            public Limb TargetLimb { get; }
            public Vector2 TargetSimPos { get; }
        }

        public struct SetAttackTargetEventData : IAttackEventData
        {
            public EventType EventType => EventType.SetAttackTarget;
            public Limb AttackLimb { get; }
            public IDamageable TargetEntity { get; }
            public Limb TargetLimb { get; }
            public Vector2 TargetSimPos { get; }
            
            public SetAttackTargetEventData(Limb attackLimb, IDamageable targetEntity, Limb targetLimb, Vector2 targetSimPos)
            {
                AttackLimb = attackLimb;
                TargetEntity = targetEntity;
                TargetLimb = targetLimb;
                TargetSimPos = targetSimPos;
            }
        }

        public struct ExecuteAttackEventData : IAttackEventData
        {
            public EventType EventType => EventType.ExecuteAttack;
            public Limb AttackLimb { get; }
            public IDamageable TargetEntity { get; }
            public Limb TargetLimb { get; }
            public Vector2 TargetSimPos { get; }
            
            public ExecuteAttackEventData(Limb attackLimb, IDamageable targetEntity, Limb targetLimb, Vector2 targetSimPos)
            {
                AttackLimb = attackLimb;
                TargetEntity = targetEntity;
                TargetLimb = targetLimb;
                TargetSimPos = targetSimPos;
            }
        }

        public struct AssignCampaignInteractionEventData : IEventData
        {
            public EventType EventType => EventType.AssignCampaignInteraction;
        }

        public struct ObjectiveManagerStateEventData : IEventData
        {
            public EventType EventType => EventType.ObjectiveManagerState;
            public readonly AIObjectiveManager.ObjectiveType ObjectiveType;
            
            public ObjectiveManagerStateEventData(AIObjectiveManager.ObjectiveType objectiveType)
            {
                ObjectiveType = objectiveType;
            }
        }

        public readonly struct LatchedOntoTargetEventData : IEventData
        {
            public EventType EventType => EventType.LatchOntoTarget;
            public readonly bool IsLatched;
            public readonly UInt16 TargetCharacterID = NullEntityID;
            public readonly UInt16 TargetStructureID = NullEntityID;
            public readonly int TargetLevelWallIndex = -1;

            public readonly Vector2 AttachSurfaceNormal = Vector2.Zero;
            public readonly Vector2 AttachPos = Vector2.Zero;

            public readonly Vector2 CharacterSimPos;

            private LatchedOntoTargetEventData(Character character, Vector2 attachSurfaceNormal, Vector2 attachPos)
            {
                CharacterSimPos = character.SimPosition;
                IsLatched = true;
                AttachSurfaceNormal = attachSurfaceNormal;
                AttachPos = attachPos;
            }

            public LatchedOntoTargetEventData(Character character, Character targetCharacter, Vector2 attachSurfaceNormal, Vector2 attachPos)
                : this(character, attachSurfaceNormal, attachPos)
            {
                TargetCharacterID = targetCharacter.ID;
            }

            public LatchedOntoTargetEventData(Character character, Structure targetStructure, Vector2 attachSurfaceNormal, Vector2 attachPos)
                : this(character, attachSurfaceNormal, attachPos)
            {
                TargetStructureID = targetStructure.ID;
            }

            public LatchedOntoTargetEventData(Character character, VoronoiCell levelWall, Vector2 attachSurfaceNormal, Vector2 attachPos)
                : this(character, attachSurfaceNormal, attachPos)
            {
                TargetLevelWallIndex = Level.Loaded.GetAllCells().IndexOf(levelWall);
            }

            /// <summary>
            /// Signifies detaching (not attached to any target)
            /// </summary>
            public LatchedOntoTargetEventData()
            {
                CharacterSimPos = Vector2.Zero;
                IsLatched = false;
            }
        }

        private struct TeamChangeEventData : IEventData
        {
            public EventType EventType => EventType.TeamChange;
        }

        [NetworkSerialize]
        public readonly record struct ItemTeamChange(CharacterTeamType TeamId, ImmutableArray<UInt16> ItemIds) : INetSerializableStruct;


        public struct AddToCrewEventData : IEventData
        {
            public EventType EventType => EventType.AddToCrew;
            public readonly ItemTeamChange ItemTeamChange;
            
            public AddToCrewEventData(CharacterTeamType teamType, IEnumerable<Item> inventoryItems)
            {
                ItemTeamChange = new ItemTeamChange(teamType, inventoryItems.Select(it => it.ID).ToImmutableArray());
            }            
        }

        public struct RemoveFromCrewEventData : IEventData
        {
            public EventType EventType => EventType.RemoveFromCrew;
            public readonly ItemTeamChange ItemTeamChange;

            public RemoveFromCrewEventData(CharacterTeamType teamType, IEnumerable<Item> inventoryItems)
            {
                ItemTeamChange = new ItemTeamChange(teamType, inventoryItems.Select(it => it.ID).ToImmutableArray());
            }
        }

        public struct UpdateExperienceEventData : IEventData
        {
            public EventType EventType => EventType.UpdateExperience;
        }

        public struct UpdateTalentsEventData : IEventData
        {
            public EventType EventType => EventType.UpdateTalents;
        }

        public struct UpdateSkillsEventData : IEventData
        {
            public readonly EventType EventType => EventType.UpdateSkills;

            public readonly bool ForceNotification;
            public readonly Identifier SkillIdentifier;

            public UpdateSkillsEventData(Identifier skillIdentifier, bool forceNotification)
            {
                SkillIdentifier = skillIdentifier;
                ForceNotification = forceNotification;
            }
        }

        private struct UpdateMoneyEventData : IEventData
        {
            public EventType EventType => EventType.UpdateMoney;
        }

        public struct UpdatePermanentStatsEventData : IEventData
        {
            public EventType EventType => EventType.UpdatePermanentStats;
            public readonly StatTypes StatType;

            public UpdatePermanentStatsEventData(StatTypes statType)
            {
                StatType = statType;
            }
        }

        public struct UpdateRefundPointsEventData : IEventData
        {
            public EventType EventType => EventType.UpdateTalentRefundPoints;
        }

        public struct ConfirmRefundEventData : IEventData
        {
            public EventType EventType => EventType.ConfirmTalentRefund;
        }
    }
}
