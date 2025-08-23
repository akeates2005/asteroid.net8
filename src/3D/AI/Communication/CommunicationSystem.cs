using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Asteroids.AI.Core;

namespace Asteroids.AI.Communication
{
    /// <summary>
    /// AI communication system for coordination and information sharing
    /// </summary>
    public class CommunicationSystem
    {
        private Queue<AIMessage> incomingMessages;
        private Queue<AIMessage> outgoingMessages;
        private List<AIMessage> messageHistory;
        private float messageProcessingInterval = 0.2f;
        private float lastProcessingTime = 0;
        private int maxHistorySize = 50;
        
        public event Action<AIMessage> OnMessageReceived;
        public event Action<AIMessage> OnMessageSent;
        
        public CommunicationSystem()
        {
            incomingMessages = new Queue<AIMessage>();
            outgoingMessages = new Queue<AIMessage>();
            messageHistory = new List<AIMessage>();
        }
        
        public void Update(float deltaTime)
        {
            lastProcessingTime += deltaTime;
            
            if (lastProcessingTime >= messageProcessingInterval)
            {
                ProcessIncomingMessages();
                ProcessOutgoingMessages();
                lastProcessingTime = 0;
            }
        }
        
        public void SendMessage(AIMessage message)
        {
            message.Timestamp = DateTime.Now;
            outgoingMessages.Enqueue(message);
        }
        
        public void BroadcastMessage(AIMessage message)
        {
            message.IsBroadcast = true;
            SendMessage(message);
        }
        
        public void ReceiveMessage(AIMessage message)
        {
            incomingMessages.Enqueue(message);
        }
        
        private void ProcessIncomingMessages()
        {
            while (incomingMessages.Count > 0)
            {
                var message = incomingMessages.Dequeue();
                ProcessMessage(message);
                
                // Store in history
                messageHistory.Add(message);
                if (messageHistory.Count > maxHistorySize)
                {
                    messageHistory.RemoveAt(0);
                }
                
                OnMessageReceived?.Invoke(message);
            }
        }
        
        private void ProcessOutgoingMessages()
        {
            while (outgoingMessages.Count > 0)
            {
                var message = outgoingMessages.Dequeue();
                
                if (message.IsBroadcast)
                {
                    // Broadcast to all nearby allies
                    CommunicationHub.Instance.BroadcastMessage(message);
                }
                else
                {
                    // Send to specific target
                    CommunicationHub.Instance.SendMessage(message);
                }
                
                OnMessageSent?.Invoke(message);
            }
        }
        
        private void ProcessMessage(AIMessage message)
        {
            switch (message.Type)
            {
                case MessageType.TargetSighted:
                    HandleTargetSighted(message);
                    break;
                case MessageType.RequestSupport:
                    HandleSupportRequest(message);
                    break;
                case MessageType.EngagingTarget:
                    HandleEngagingTarget(message);
                    break;
                case MessageType.AllyDestroyed:
                    HandleAllyDestroyed(message);
                    break;
                case MessageType.FormationOrder:
                    HandleFormationOrder(message);
                    break;
                case MessageType.TacticalOrder:
                    HandleTacticalOrder(message);
                    break;
                case MessageType.StatusUpdate:
                    HandleStatusUpdate(message);
                    break;
                case MessageType.CoordinatedAttack:
                    HandleCoordinatedAttack(message);
                    break;
                case MessageType.RequestEscort:
                    HandleEscortRequest(message);
                    break;
                case MessageType.IntelReport:
                    HandleIntelReport(message);
                    break;
            }
        }
        
        #region Message Handlers
        
        private void HandleTargetSighted(AIMessage message)
        {
            // Share target information with nearby allies
            var targetInfo = message.Data as dynamic;
            
            // Update threat database
            ThreatDatabase.Instance.UpdateThreat(message.Position, targetInfo);
            
            // Notify formation if applicable
            if (message.Sender.Formation != null)
            {
                NotifyFormationOfTarget(message.Sender.Formation, message.Position);
            }
        }
        
        private void HandleSupportRequest(AIMessage message)
        {
            // Evaluate if we can provide support
            var requester = message.Sender;
            var priority = CalculateSupportPriority(requester);
            
            if (priority > 0.5f)
            {
                // Send confirmation and move to support
                var response = new AIMessage
                {
                    Type = MessageType.SupportConfirmed,
                    Sender = null, // Would be set by the responding ship
                    Target = requester,
                    Position = message.Position,
                    Data = "en_route"
                };
                
                SendMessage(response);
            }
        }
        
        private void HandleEngagingTarget(AIMessage message)
        {
            // Update formation tactics based on engagement
            if (message.Sender.Formation != null)
            {
                var formation = message.Sender.Formation;
                formation.SetDestination(message.Position);
                
                // Alert other formation members
                foreach (var member in formation.Members)
                {
                    if (member.Ship != message.Sender)
                    {
                        member.Ship.SetTarget(message.Data as AIEnemyShip);
                    }
                }
            }
        }
        
        private void HandleAllyDestroyed(AIMessage message)
        {
            // Update threat assessment
            ThreatDatabase.Instance.IncreaseThreatLevel(message.Position, 0.3f);
            
            // Trigger revenge/retaliation behaviors
            var allies = GetNearbyAllies(message.Position, 100f);
            foreach (var ally in allies)
            {
                ally.Aggressiveness = Math.Min(1f, ally.Aggressiveness + 0.2f);
            }
        }
        
        private void HandleFormationOrder(AIMessage message)
        {
            var orderData = message.Data as FormationOrderData;
            if (orderData == null) return;
            
            // Execute formation change
            switch (orderData.OrderType)
            {
                case FormationOrderType.ChangeFormation:
                    ChangeFormation(orderData.NewFormationType);
                    break;
                case FormationOrderType.SpreadOut:
                    IncreaseFormationSpacing();
                    break;
                case FormationOrderType.CloseRanks:
                    DecreaseFormationSpacing();
                    break;
                case FormationOrderType.BreakFormation:
                    BreakFormation();
                    break;
            }
        }
        
        private void HandleTacticalOrder(AIMessage message)
        {
            var tacticName = message.Data as string;
            
            // Execute tactical maneuver
            switch (tacticName)
            {
                case "direct_assault":
                    ExecuteDirectAssault(message.Position);
                    break;
                case "flanking_maneuver":
                    ExecuteFlankingManeuver(message.Position);
                    break;
                case "defensive_formation":
                    ExecuteDefensiveFormation();
                    break;
                case "hit_and_run":
                    ExecuteHitAndRun(message.Position);
                    break;
            }
        }
        
        private void HandleStatusUpdate(AIMessage message)
        {
            var statusData = message.Data as StatusData;
            if (statusData == null) return;
            
            // Update ally status tracking
            AllyTracker.Instance.UpdateAllyStatus(message.Sender, statusData);
        }
        
        private void HandleCoordinatedAttack(AIMessage message)
        {
            var attackType = message.Data as string;
            
            // Synchronize attack timing
            switch (attackType)
            {
                case "bombardment_ready":
                    ConfirmBombardmentReadiness();
                    break;
                case "interceptor_strike":
                    ConfirmInterceptorStrike();
                    break;
                case "flanking_complete":
                    ExecuteFlankingAttack();
                    break;
            }
        }
        
        private void HandleEscortRequest(AIMessage message)
        {
            var healthRatio = (float)message.Data;
            
            // Evaluate escort capability
            if (CanProvideEscort(message.Sender))
            {
                var response = new AIMessage
                {
                    Type = MessageType.EscortConfirmed,
                    Target = message.Sender,
                    Position = message.Position,
                    Data = "providing_escort"
                };
                
                SendMessage(response);
            }
        }
        
        private void HandleIntelReport(AIMessage message)
        {
            var intel = message.Data as IntelData;
            if (intel == null) return;
            
            // Update intelligence database
            IntelligenceDatabase.Instance.ProcessIntel(intel);
        }
        
        #endregion
        
        #region Helper Methods
        
        private float CalculateSupportPriority(AIEnemyShip requester)
        {
            if (requester == null) return 0f;
            
            // Calculate priority based on various factors
            var healthFactor = 1f - (requester.Health / requester.MaxHealth);
            var distanceFactor = 1f - Math.Min(1f, Vector3.Distance(Vector3.Zero, requester.Position) / 200f);
            var shipTypeFactor = GetShipTypePriority(requester.ShipType);
            
            return (healthFactor * 0.4f + distanceFactor * 0.3f + shipTypeFactor * 0.3f);
        }
        
        private float GetShipTypePriority(EnemyShipType shipType)
        {
            return shipType switch
            {
                EnemyShipType.Bomber => 1.0f,     // High priority - valuable asset
                EnemyShipType.Fighter => 0.8f,    // Medium-high priority
                EnemyShipType.Scout => 0.6f,      // Medium priority
                EnemyShipType.Interceptor => 0.4f, // Lower priority - fast escape
                _ => 0.5f
            };
        }
        
        private List<AIEnemyShip> GetNearbyAllies(Vector3 position, float radius)
        {
            // This would integrate with the AI manager to get nearby allies
            return new List<AIEnemyShip>();
        }
        
        private void NotifyFormationOfTarget(Formations.FormationController formation, Vector3 targetPosition)
        {
            formation.SetDestination(targetPosition);
        }
        
        private void ChangeFormation(Formations.FormationType formationType)
        {
            // Implementation would change formation type
        }
        
        private void IncreaseFormationSpacing()
        {
            // Implementation would increase formation scale
        }
        
        private void DecreaseFormationSpacing()
        {
            // Implementation would decrease formation scale
        }
        
        private void BreakFormation()
        {
            // Implementation would dissolve current formation
        }
        
        private void ExecuteDirectAssault(Vector3 targetPosition)
        {
            // Implementation would coordinate direct assault
        }
        
        private void ExecuteFlankingManeuver(Vector3 targetPosition)
        {
            // Implementation would coordinate flanking
        }
        
        private void ExecuteDefensiveFormation()
        {
            // Implementation would form defensive posture
        }
        
        private void ExecuteHitAndRun(Vector3 targetPosition)
        {
            // Implementation would coordinate hit and run
        }
        
        private void ConfirmBombardmentReadiness()
        {
            // Implementation would confirm ready for bombardment
        }
        
        private void ConfirmInterceptorStrike()
        {
            // Implementation would confirm interceptor strike readiness
        }
        
        private void ExecuteFlankingAttack()
        {
            // Implementation would execute flanking attack
        }
        
        private bool CanProvideEscort(AIEnemyShip requester)
        {
            // Evaluate if this ship can provide escort
            return true; // Simplified for now
        }
        
        #endregion
        
        public List<AIMessage> GetMessageHistory(MessageType? typeFilter = null)
        {
            if (typeFilter.HasValue)
            {
                return messageHistory.Where(m => m.Type == typeFilter.Value).ToList();
            }
            
            return new List<AIMessage>(messageHistory);
        }
        
        public void ClearMessageHistory()
        {
            messageHistory.Clear();
        }
    }
    
    /// <summary>
    /// Central hub for AI communication
    /// </summary>
    public class CommunicationHub
    {
        private static CommunicationHub instance;
        public static CommunicationHub Instance => instance ??= new CommunicationHub();
        
        private List<AIEnemyShip> registeredShips;
        private float communicationRange = 150f;
        
        private CommunicationHub()
        {
            registeredShips = new List<AIEnemyShip>();
        }
        
        public void RegisterShip(AIEnemyShip ship)
        {
            if (!registeredShips.Contains(ship))
            {
                registeredShips.Add(ship);
            }
        }
        
        public void UnregisterShip(AIEnemyShip ship)
        {
            registeredShips.Remove(ship);
        }
        
        public void BroadcastMessage(AIMessage message)
        {
            var sender = message.Sender;
            var recipients = registeredShips.Where(ship => 
                ship != sender && 
                Vector3.Distance(ship.Position, sender.Position) <= communicationRange
            ).ToList();
            
            foreach (var recipient in recipients)
            {
                recipient.Communication.ReceiveMessage(message);
            }
        }
        
        public void SendMessage(AIMessage message)
        {
            if (message.Target != null)
            {
                message.Target.Communication.ReceiveMessage(message);
            }
        }
        
        public void SetCommunicationRange(float range)
        {
            communicationRange = range;
        }
    }
    
    /// <summary>
    /// AI message structure
    /// </summary>
    public class AIMessage
    {
        public MessageType Type { get; set; }
        public AIEnemyShip Sender { get; set; }
        public AIEnemyShip Target { get; set; }
        public Vector3 Position { get; set; }
        public object Data { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsBroadcast { get; set; }
        public float Priority { get; set; } = 0.5f;
    }
    
    public enum MessageType
    {
        TargetSighted,
        RequestSupport,
        SupportConfirmed,
        EngagingTarget,
        AllyDestroyed,
        FormationOrder,
        TacticalOrder,
        StatusUpdate,
        CoordinatedAttack,
        RequestEscort,
        EscortConfirmed,
        IntelReport
    }
    
    public class FormationOrderData
    {
        public FormationOrderType OrderType { get; set; }
        public Formations.FormationType NewFormationType { get; set; }
        public float SpacingMultiplier { get; set; } = 1f;
    }
    
    public enum FormationOrderType
    {
        ChangeFormation,
        SpreadOut,
        CloseRanks,
        BreakFormation
    }
    
    public class StatusData
    {
        public float HealthRatio { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public bool InCombat { get; set; }
        public float AmmoLevel { get; set; }
    }
    
    public class IntelData
    {
        public Vector3 Position { get; set; }
        public EnemyShipType ShipType { get; set; }
        public float ThreatLevel { get; set; }
        public Vector3 Velocity { get; set; }
        public float Confidence { get; set; }
    }
    
    /// <summary>
    /// Tracks threat information
    /// </summary>
    public class ThreatDatabase
    {
        private static ThreatDatabase instance;
        public static ThreatDatabase Instance => instance ??= new ThreatDatabase();
        
        private Dictionary<Vector3, ThreatInfo> threats;
        
        private ThreatDatabase()
        {
            threats = new Dictionary<Vector3, ThreatInfo>();
        }
        
        public void UpdateThreat(Vector3 position, object threatData)
        {
            // Update threat information
        }
        
        public void IncreaseThreatLevel(Vector3 position, float increase)
        {
            // Increase threat level at position
        }
    }
    
    public class ThreatInfo
    {
        public float ThreatLevel { get; set; }
        public DateTime LastSeen { get; set; }
        public object Data { get; set; }
    }
    
    /// <summary>
    /// Tracks ally status and coordination
    /// </summary>
    public class AllyTracker
    {
        private static AllyTracker instance;
        public static AllyTracker Instance => instance ??= new AllyTracker();
        
        private Dictionary<AIEnemyShip, StatusData> allyStatuses;
        
        private AllyTracker()
        {
            allyStatuses = new Dictionary<AIEnemyShip, StatusData>();
        }
        
        public void UpdateAllyStatus(AIEnemyShip ally, StatusData status)
        {
            allyStatuses[ally] = status;
        }
    }
    
    /// <summary>
    /// Processes and stores intelligence data
    /// </summary>
    public class IntelligenceDatabase
    {
        private static IntelligenceDatabase instance;
        public static IntelligenceDatabase Instance => instance ??= new IntelligenceDatabase();
        
        private List<IntelData> intelReports;
        
        private IntelligenceDatabase()
        {
            intelReports = new List<IntelData>();
        }
        
        public void ProcessIntel(IntelData intel)
        {
            intelReports.Add(intel);
            
            // Process and correlate intelligence
            // This could update threat assessments, target priorities, etc.
        }
    }
}