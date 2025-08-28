# Lives System Architecture Diagrams

## System Component Relationships

```mermaid
graph TB
    subgraph "Lives System Core"
        LM[LivesManager]
        LS[PlayerLifeState]
        LD[LivesData]
        SRC[SafeRespawnCalculator]
    end
    
    subgraph "Existing Game Systems"
        GP[GameProgram]
        P[Player]
        SG[SpatialGrid]
        PUM[PowerUpManager]
        AHD[AnimatedHUD]
        AM[AudioManager]
    end
    
    subgraph "Collision System"
        CD[CollisionDetection]
        CDH[CollisionDeathHandler]
    end
    
    subgraph "UI & Effects"
        LD_UI[LivesDisplay]
        PE[ParticleEffects]
        VE[VisualEffects]
    end
    
    %% Core relationships
    LM --> LD
    LM --> LS
    LM --> SRC
    
    %% Integration points
    GP --> LM
    LM --> P
    SRC --> SG
    LM --> PUM
    LM --> AHD
    LM --> AM
    
    %% Collision integration
    GP --> CD
    CD --> CDH
    CDH --> LM
    
    %% UI integration
    AHD --> LD_UI
    LD_UI --> LD
    LM --> PE
    LM --> VE
    
    %% Data flow
    P -.->|State Query| LM
    LM -.->|Death Event| CDH
    SG -.->|Position Data| SRC
    LD -.->|UI Updates| LD_UI
```

## State Transition Diagram

```mermaid
stateDiagram-v2
    [*] --> Alive
    
    Alive --> Dying: Collision Detected
    Dying --> Dead: Death Animation Complete
    Dead --> Respawning: Lives > 0 & Respawn Timer Expired
    Dead --> [*]: Lives = 0 (Game Over)
    
    Respawning --> Invulnerable: Safe Position Found
    Invulnerable --> Alive: Invulnerability Timer Expired
    
    Alive --> Alive: Shield Active (Collision Ignored)
    Invulnerable --> Invulnerable: Collision Ignored
    
    note right of Dying
        - Death particles spawn
        - Death sound plays
        - Lives decremented
    end note
    
    note right of Respawning
        - Calculate safe position
        - Spawn animation
        - Respawn sound
    end note
    
    note right of Invulnerable
        - Visual flashing effect
        - Collision immunity
        - Timer countdown
    end note
```

## Collision Integration Flow

```mermaid
sequenceDiagram
    participant GP as GameProgram
    participant CD as CollisionDetection
    participant CDH as CollisionDeathHandler
    participant LM as LivesManager
    participant P as Player
    participant UI as AnimatedHUD
    participant SFX as AudioManager
    
    GP->>CD: CheckCollisions()
    CD->>CDH: PlayerCollision(asteroid)
    
    alt Shield Active or Invulnerable
        CDH->>LM: ShouldIgnoreCollisions()
        LM-->>CDH: true
        CDH->>GP: No damage taken
    else Normal collision
        CDH->>LM: HandlePlayerDeath(position)
        LM->>LM: SetState(Dying)
        LM->>LM: DecrementLives()
        LM->>SFX: PlayDeathSound()
        LM->>UI: UpdateLivesDisplay()
        
        par Death Animation
            LM->>P: StartDeathAnimation()
        and Particle Effects
            LM->>GP: SpawnDeathParticles()
        end
        
        LM->>LM: SetState(Dead)
        LM->>LM: StartRespawnTimer()
    end
```

## Respawn Position Algorithm Flow

```mermaid
flowchart TD
    Start([Player Death Event]) --> Center{Is Screen Center Safe?}
    
    Center -->|Yes| UseCenter[Use Screen Center]
    Center -->|No| Spiral[Try Spiral Pattern]
    
    Spiral --> CheckRadius{Check Position at Radius R}
    CheckRadius -->|Safe Found| UseSafe[Use Safe Position]
    CheckRadius -->|No Safe Position| IncRadius[R = R + 25]
    
    IncRadius --> MaxRadius{R > 300?}
    MaxRadius -->|No| CheckRadius
    MaxRadius -->|Yes| Emergency[Create Emergency Safe Zone]
    
    Emergency --> ForceCenter[Clear Threats from Center]
    ForceCenter --> UseCenter
    
    UseCenter --> SpawnPlayer[Spawn Player with Invulnerability]
    UseSafe --> SpawnPlayer
    
    SpawnPlayer --> End([Respawn Complete])
    
    subgraph Safety Checks
        SC1[Check Distance to Asteroids > 80px]
        SC2[Check Distance to Enemies > 120px]
        SC3[Check Distance to Bullets > 30px]
    end
    
    CheckRadius -.-> SC1
    SC1 -.-> SC2
    SC2 -.-> SC3
```

## Data Flow Architecture

```mermaid
graph LR
    subgraph "Input Events"
        CE[Collision Events]
        UI_E[UI Events]
        PU[PowerUp Events]
    end
    
    subgraph "Lives Manager Core"
        LM[LivesManager]
        LD[LivesData]
    end
    
    subgraph "Output Systems"
        GS[Game State]
        UI_S[UI System]
        AS[Audio System]
        PS[Particle System]
    end
    
    CE --> LM
    UI_E --> LM
    PU --> LM
    
    LM <--> LD
    
    LM --> GS
    LM --> UI_S
    LM --> AS
    LM --> PS
    
    LD -.->|State Queries| GS
    LD -.->|Display Data| UI_S
```

## Memory Management Architecture

```mermaid
graph TB
    subgraph "Memory Pools"
        PP[Particle Pool]
        EP[Effect Pool]
        AP[Audio Pool]
    end
    
    subgraph "Cache Systems"
        RPC[Respawn Position Cache]
        SC[State Cache]
        EC[Event Cache]
    end
    
    subgraph "Lives System"
        LM[LivesManager]
        SRC[SafeRespawnCalculator]
        LD_UI[LivesDisplay]
    end
    
    LM --> PP
    LM --> EP
    LM --> AP
    
    SRC --> RPC
    LM --> SC
    LD_UI --> EC
    
    PP -.->|Reuse| LM
    EP -.->|Reuse| LM
    RPC -.->|Cache Hit| SRC
```

## Integration Timeline

```mermaid
gantt
    title Lives System Implementation Timeline
    dateFormat X
    axisFormat %s
    
    section Phase 1: Foundation
    Data Models     :done, p1a, 0, 2h
    Core Enums      :done, p1b, after p1a, 1h
    
    section Phase 2: Core Logic
    Lives Manager   :active, p2a, after p1b, 3h
    State Management:p2b, after p2a, 1h
    
    section Phase 3: Respawn System
    Position Algorithm :p3a, after p2b, 2h
    Spatial Integration :p3b, after p3a, 2h
    Safety Validation :p3c, after p3b, 1h
    
    section Phase 4: Integration
    Collision System :p4a, after p3c, 2h
    Player Updates  :p4b, after p4a, 1h
    Shield Integration :p4c, after p4b, 1h
    
    section Phase 5: UI & Effects
    Lives Display   :p5a, after p4c, 1h
    Visual Effects  :p5b, after p5a, 1h
    Audio Integration :p5c, after p5b, 1h
    
    section Phase 6: Testing
    Unit Tests      :p6a, after p5c, 1h
    Integration Tests :p6b, after p6a, 1h
```