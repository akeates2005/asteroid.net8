# File Organization Structure - Technical Specification

## Overview

This document defines the comprehensive file organization structure for Phase 2 of the Asteroids game project. The structure follows industry best practices for C# game development, emphasizing modularity, maintainability, and scalability.

## Root Directory Structure

```
Asteroids/
├── 📁 src/                          # Source code (organized by feature/system)
│   ├── 📁 Core/                     # Core game systems
│   ├── 📁 Collision/                # Collision detection system
│   ├── 📁 Spatial/                  # Spatial partitioning system
│   ├── 📁 ObjectPool/               # Object pooling system
│   ├── 📁 Particles/                # Particle system
│   ├── 📁 Performance/              # Performance monitoring
│   ├── 📁 Resilience/               # Error handling & resilience
│   ├── 📁 GameLogic/                # Game-specific logic
│   ├── 📁 Rendering/                # Rendering systems
│   ├── 📁 Audio/                    # Audio systems
│   ├── 📁 Input/                    # Input handling
│   ├── 📁 UI/                       # User interface
│   ├── 📁 Utils/                    # Utility classes
│   └── 📁 3D/                       # 3D-specific implementations
├── 📁 tests/                        # Test code (mirrors src structure)
│   ├── 📁 Unit/                     # Unit tests
│   ├── 📁 Integration/              # Integration tests
│   ├── 📁 Performance/              # Performance tests
│   ├── 📁 EndToEnd/                 # End-to-end tests
│   └── 📁 TestUtilities/            # Test helpers and utilities
├── 📁 benchmarks/                   # Performance benchmarks
├── 📁 docs/                         # Documentation
│   ├── 📁 architecture/             # Architecture documentation
│   ├── 📁 api/                      # API documentation
│   ├── 📁 guides/                   # Developer guides
│   ├── 📁 phase2-architecture/      # Phase 2 specifications
│   └── 📁 diagrams/                 # Architecture diagrams
├── 📁 config/                       # Configuration files
│   ├── 📁 development/              # Development config
│   ├── 📁 testing/                  # Testing config
│   └── 📁 production/               # Production config
├── 📁 assets/                       # Game assets
│   ├── 📁 textures/                 # Texture files
│   ├── 📁 sounds/                   # Sound files
│   ├── 📁 fonts/                    # Font files
│   └── 📁 data/                     # Game data files
├── 📁 tools/                        # Development tools
│   ├── 📁 scripts/                  # Build/utility scripts
│   ├── 📁 analyzers/                # Code analyzers
│   └── 📁 generators/               # Code generators
├── 📁 build/                        # Build output (gitignored)
├── 📁 packages/                     # NuGet packages (gitignored)
├── 📁 logs/                         # Application logs (gitignored)
├── 📄 Asteroids.sln                 # Solution file
├── 📄 Asteroids.csproj              # Main project file
├── 📄 Directory.Build.props         # MSBuild properties
├── 📄 Directory.Build.targets       # MSBuild targets
├── 📄 .editorconfig                 # Code style settings
├── 📄 .gitignore                    # Git ignore rules
├── 📄 .gitattributes               # Git attributes
├── 📄 README.md                     # Project overview
├── 📄 CHANGELOG.md                  # Change history
└── 📄 LICENSE                       # License file
```

## Source Code Organization (`src/`)

### Core System Structure (`src/Core/`)

```
src/Core/
├── 📁 GameEngine/                   # Core game engine
│   ├── 📄 GameEngine.cs            # Main game engine class
│   ├── 📄 GameLoop.cs              # Game loop implementation
│   ├── 📄 TimeManager.cs           # Time and frame management
│   └── 📄 GameStateManager.cs      # Game state management
├── 📁 SystemManager/               # System management
│   ├── 📄 IGameSystem.cs           # Game system interface
│   ├── 📄 GameSystemManager.cs     # System lifecycle manager
│   ├── 📄 SystemDependencies.cs    # System dependency resolution
│   └── 📄 SystemPriority.cs        # System update priorities
├── 📁 Configuration/               # Configuration management
│   ├── 📄 IConfigurationManager.cs # Configuration interface
│   ├── 📄 ConfigurationManager.cs  # Configuration implementation
│   ├── 📄 GameSettings.cs          # Game settings model
│   └── 📄 SystemConfiguration.cs   # System-specific config
├── 📁 Events/                      # Event system
│   ├── 📄 IEventBus.cs            # Event bus interface
│   ├── 📄 EventBus.cs             # Event bus implementation
│   ├── 📄 GameEvents.cs           # Game event definitions
│   └── 📄 EventHandlers.cs        # Event handler utilities
├── 📁 Mathematics/                 # Math utilities
│   ├── 📄 MathUtils.cs            # General math utilities
│   ├── 📄 VectorExtensions.cs     # Vector extension methods
│   ├── 📄 GeometryUtils.cs        # Geometry calculations
│   └── 📄 RandomUtils.cs          # Random number utilities
└── 📁 Extensions/                  # Extension methods
    ├── 📄 CollectionExtensions.cs  # Collection extensions
    ├── 📄 StringExtensions.cs      # String extensions
    └── 📄 EnumExtensions.cs        # Enum extensions
```

### Collision Detection System (`src/Collision/`)

```
src/Collision/
├── 📁 Interfaces/                  # Core interfaces
│   ├── 📄 ICollidable.cs          # Collidable object interface
│   ├── 📄 ICollisionDetector.cs   # Collision detector interface
│   ├── 📄 IBroadPhaseDetector.cs  # Broad phase interface
│   └── 📄 INarrowPhaseDetector.cs # Narrow phase interface
├── 📁 BroadPhase/                 # Broad phase implementations
│   ├── 📄 SpatialGridBroadPhase.cs      # Spatial grid implementation
│   ├── 📄 QuadTreeBroadPhase.cs         # QuadTree implementation
│   ├── 📄 SweepAndPruneBroadPhase.cs    # Sweep and prune implementation
│   └── 📄 BroadPhaseFactory.cs          # Broad phase factory
├── 📁 NarrowPhase/                # Narrow phase implementations
│   ├── 📄 CircleCircleDetector.cs       # Circle-circle collision
│   ├── 📄 CirclePolygonDetector.cs      # Circle-polygon collision
│   ├── 📄 PolygonPolygonDetector.cs     # Polygon-polygon collision
│   └── 📄 NarrowPhaseFactory.cs         # Narrow phase factory
├── 📁 Resolution/                 # Collision resolution
│   ├── 📄 ICollisionResolver.cs   # Resolution interface
│   ├── 📄 ImpulseBasedResolver.cs # Impulse-based resolution
│   ├── 📄 PositionBasedResolver.cs# Position-based resolution
│   └── 📄 ContactManifold.cs      # Contact manifold
├── 📁 Shapes/                     # Collision shapes
│   ├── 📄 ICollisionShape.cs      # Shape interface
│   ├── 📄 CircleShape.cs          # Circle shape
│   ├── 📄 PolygonShape.cs         # Polygon shape
│   ├── 📄 AABBShape.cs            # AABB shape
│   └── 📄 CompoundShape.cs        # Compound shape
├── 📁 Data/                       # Data structures
│   ├── 📄 CollisionInfo.cs        # Collision information
│   ├── 📄 CollisionPair.cs        # Collision pair
│   ├── 📄 BoundingBox.cs          # Bounding box
│   └── 📄 CollisionLayers.cs      # Collision layers
├── 📄 CollisionDetectionSystem.cs # Main collision system
├── 📄 CollisionManager.cs         # Collision manager
└── 📄 CollisionConfiguration.cs   # Configuration settings
```

### Spatial Partitioning System (`src/Spatial/`)

```
src/Spatial/
├── 📁 Interfaces/                 # Core interfaces
│   ├── 📄 ISpatialStructure.cs    # Spatial structure interface
│   ├── 📄 ISpatialObject.cs       # Spatial object interface
│   └── 📄 ISpatialQuery.cs        # Query interface
├── 📁 Grid/                       # Grid-based structures
│   ├── 📄 SpatialGrid.cs          # Basic spatial grid
│   ├── 📄 AdaptiveSpatialGrid.cs  # Adaptive grid
│   ├── 📄 HierarchicalGrid.cs     # Multi-level grid
│   └── 📄 GridCell.cs             # Grid cell implementation
├── 📁 QuadTree/                   # QuadTree implementations
│   ├── 📄 QuadTree.cs             # Basic QuadTree
│   ├── 📄 DynamicQuadTree.cs      # Dynamic QuadTree
│   ├── 📄 LooseQuadTree.cs        # Loose QuadTree
│   └── 📄 QuadTreeNode.cs         # QuadTree node
├── 📁 RTree/                      # R-Tree implementations
│   ├── 📄 RTree.cs                # R-Tree implementation
│   ├── 📄 RTreeNode.cs            # R-Tree node
│   └── 📄 RTreeBounds.cs          # Bounds calculation
├── 📁 BSP/                        # BSP Tree implementations
│   ├── 📄 BSPTree.cs              # BSP Tree
│   ├── 📄 BSPNode.cs              # BSP Node
│   └── 📄 BSPPartitioner.cs       # Space partitioner
├── 📁 Cache/                      # Query caching
│   ├── 📄 SpatialQueryCache.cs    # Query cache
│   ├── 📄 CacheKey.cs             # Cache key
│   └── 📄 CacheMetrics.cs         # Cache metrics
├── 📁 Data/                       # Data structures
│   ├── 📄 SpatialBounds.cs        # Spatial bounds
│   ├── 📄 SpatialLayers.cs        # Spatial layers
│   └── 📄 QueryResult.cs          # Query results
├── 📄 SpatialManager.cs           # Main spatial manager
└── 📄 SpatialConfiguration.cs     # Configuration
```

### Object Pooling System (`src/ObjectPool/`)

```
src/ObjectPool/
├── 📁 Core/                       # Core pooling
│   ├── 📄 IObjectPool.cs          # Pool interface
│   ├── 📄 IPoolable.cs            # Poolable interface
│   ├── 📄 ObjectPool.cs           # Generic pool
│   ├── 📄 AdvancedObjectPool.cs   # Advanced pool
│   └── 📄 PoolConfiguration.cs    # Pool configuration
├── 📁 Specialized/                # Specialized pools
│   ├── 📄 BulletPool.cs           # Bullet object pool
│   ├── 📄 AsteroidPool.cs         # Asteroid object pool
│   ├── 📄 ParticlePool.cs         # Particle object pool
│   └── 📄 EffectPool.cs           # Effect object pool
├── 📁 Management/                 # Pool management
│   ├── 📄 PoolManager.cs          # Global pool manager
│   ├── 📄 PoolRegistry.cs         # Pool registration
│   ├── 📄 PoolMetrics.cs          # Pool metrics
│   └── 📄 PoolStrategies.cs       # Allocation strategies
├── 📁 Strategies/                 # Pooling strategies
│   ├── 📄 IPoolingStrategy.cs     # Strategy interface
│   ├── 📄 AdaptiveStrategy.cs     # Adaptive strategy
│   ├── 📄 FixedSizeStrategy.cs    # Fixed size strategy
│   └── 📄 PreallocationStrategy.cs# Preallocation strategy
└── 📄 PoolingExtensions.cs        # Extension methods
```

### Particle System (`src/Particles/`)

```
src/Particles/
├── 📁 Core/                       # Core particle system
│   ├── 📄 IParticle.cs            # Particle interface
│   ├── 📄 IParticleSystem.cs      # System interface
│   ├── 📄 BaseParticle.cs         # Base particle class
│   ├── 📄 ParticlePool.cs         # Particle pooling
│   └── 📄 ParticleDataManager.cs  # Data management
├── 📁 Systems/                    # Particle systems
│   ├── 📄 ExplosionSystem.cs      # Explosion particles
│   ├── 📄 EngineTrailSystem.cs    # Engine trails
│   ├── 📄 WeaponEffectSystem.cs   # Weapon effects
│   ├── 📄 EnvironmentalSystem.cs  # Environmental particles
│   └── 📄 ImpactEffectSystem.cs   # Impact effects
├── 📁 Effects/                    # Specific effects
│   ├── 📄 ExplosionParticle.cs    # Explosion particle
│   ├── 📄 EngineTrailParticle.cs  # Engine trail particle
│   ├── 📄 SparkParticle.cs        # Spark particle
│   └── 📄 SmokeParticle.cs        # Smoke particle
├── 📁 Rendering/                  # Particle rendering
│   ├── 📄 IParticleRenderer.cs    # Renderer interface
│   ├── 📄 BatchParticleRenderer.cs# Batch renderer
│   ├── 📄 InstancedRenderer.cs    # Instanced renderer
│   └── 📄 ParticleMaterial.cs     # Particle materials
├── 📁 Physics/                    # Particle physics
│   ├── 📄 ParticlePhysics.cs      # Physics simulation
│   ├── 📄 ForceGenerators.cs      # Force generators
│   └── 📄 Integrators.cs          # Integration methods
├── 📁 Optimization/               # Performance optimization
│   ├── 📄 ParticleLOD.cs          # Level of detail
│   ├── 📄 ParticleCulling.cs      # Frustum culling
│   └── 📄 GPUSimulation.cs        # GPU acceleration
└── 📄 MasterParticleSystem.cs     # Master system manager
```

### Performance Monitoring (`src/Performance/`)

```
src/Performance/
├── 📁 Monitoring/                 # Performance monitoring
│   ├── 📄 IPerformanceMonitor.cs  # Monitor interface
│   ├── 📄 PerformanceMonitor.cs   # Main monitor
│   ├── 📄 MetricsCollector.cs     # Metrics collection
│   └── 📄 PerformanceMetric.cs    # Metric implementation
├── 📁 Profiling/                  # Profiling tools
│   ├── 📄 IProfiler.cs            # Profiler interface
│   ├── 📄 FrameProfiler.cs        # Frame profiling
│   ├── 📄 MemoryProfiler.cs       # Memory profiling
│   └── 📄 CPUProfiler.cs          # CPU profiling
├── 📁 Benchmarks/                 # Benchmarking
│   ├── 📄 IBenchmark.cs           # Benchmark interface
│   ├── 📄 BenchmarkSuite.cs       # Benchmark suite
│   ├── 📄 PerformanceBenchmark.cs # Performance benchmark
│   └── 📄 BenchmarkRunner.cs      # Benchmark runner
├── 📁 Analysis/                   # Performance analysis
│   ├── 📄 PerformanceAnalyzer.cs  # Analysis engine
│   ├── 📄 BottleneckDetector.cs   # Bottleneck detection
│   ├── 📄 TrendAnalyzer.cs        # Trend analysis
│   └── 📄 ReportGenerator.cs      # Report generation
├── 📁 Optimization/               # Optimization tools
│   ├── 📄 OptimizationEngine.cs   # Optimization engine
│   ├── 📄 AdaptiveQuality.cs      # Quality adjustment
│   └── 📄 ResourceAllocator.cs    # Resource allocation
└── 📄 PerformanceConfiguration.cs # Performance config
```

### Error Handling & Resilience (`src/Resilience/`)

```
src/Resilience/
├── 📁 ErrorHandling/              # Error handling
│   ├── 📄 IErrorHandler.cs        # Error handler interface
│   ├── 📄 ErrorHandler.cs         # Main error handler
│   ├── 📄 ErrorStrategy.cs        # Error strategies
│   └── 📄 ErrorContext.cs         # Error context
├── 📁 Recovery/                   # Recovery mechanisms
│   ├── 📄 IRecoveryMechanism.cs   # Recovery interface
│   ├── 📄 GameStateRecovery.cs    # Game state recovery
│   ├── 📄 ComponentRecovery.cs    # Component recovery
│   └── 📄 AutoRecovery.cs         # Automatic recovery
├── 📁 Patterns/                   # Resilience patterns
│   ├── 📄 CircuitBreaker.cs       # Circuit breaker
│   ├── 📄 RetryPolicy.cs          # Retry policies
│   ├── 📄 BulkheadPattern.cs      # Bulkhead isolation
│   └── 📄 FallbackMechanism.cs    # Fallback handling
├── 📁 Monitoring/                 # Health monitoring
│   ├── 📄 HealthMonitor.cs        # Health monitoring
│   ├── 📄 ComponentHealth.cs      # Component health
│   └── 📄 SystemDiagnostics.cs    # System diagnostics
└── 📄 ResilienceConfiguration.cs  # Resilience config
```

### Game Logic (`src/GameLogic/`)

```
src/GameLogic/
├── 📁 Entities/                   # Game entities
│   ├── 📄 IGameEntity.cs          # Entity interface
│   ├── 📄 Player.cs               # Player entity
│   ├── 📄 Asteroid.cs             # Asteroid entity
│   ├── 📄 Bullet.cs               # Bullet entity
│   └── 📄 PowerUp.cs              # Power-up entity
├── 📁 Components/                 # Entity components
│   ├── 📄 IComponent.cs           # Component interface
│   ├── 📄 TransformComponent.cs   # Position/rotation
│   ├── 📄 PhysicsComponent.cs     # Physics properties
│   ├── 📄 RenderComponent.cs      # Rendering data
│   └── 📄 CollisionComponent.cs   # Collision data
├── 📁 Systems/                    # Game systems
│   ├── 📄 MovementSystem.cs       # Movement processing
│   ├── 📄 WeaponSystem.cs         # Weapon handling
│   ├── 📄 SpawningSystem.cs       # Entity spawning
│   └── 📄 ScoreSystem.cs          # Score tracking
├── 📁 States/                     # Game states
│   ├── 📄 IGameState.cs           # Game state interface
│   ├── 📄 MainMenuState.cs        # Main menu
│   ├── 📄 PlayingState.cs         # Playing state
│   ├── 📄 PausedState.cs          # Paused state
│   └── 📄 GameOverState.cs        # Game over state
└── 📄 GameManager.cs              # Main game manager
```

## Test Structure (`tests/`)

```
tests/
├── 📁 Unit/                       # Unit tests (mirrors src structure)
│   ├── 📁 Core/                   # Core system tests
│   ├── 📁 Collision/              # Collision tests
│   ├── 📁 Spatial/                # Spatial tests
│   ├── 📁 ObjectPool/             # Pool tests
│   ├── 📁 Particles/              # Particle tests
│   ├── 📁 Performance/            # Performance tests
│   ├── 📁 Resilience/             # Resilience tests
│   └── 📁 GameLogic/              # Game logic tests
├── 📁 Integration/                # Integration tests
│   ├── 📄 SystemIntegrationTests.cs    # System integration
│   ├── 📄 PerformanceIntegration.cs    # Performance integration
│   └── 📄 EndToEndScenarios.cs         # End-to-end scenarios
├── 📁 Performance/                # Performance-specific tests
│   ├── 📄 BenchmarkTests.cs       # Benchmark tests
│   ├── 📄 LoadTests.cs            # Load testing
│   ├── 📄 StressTests.cs          # Stress testing
│   └── 📄 MemoryTests.cs          # Memory testing
├── 📁 EndToEnd/                   # End-to-end tests
│   ├── 📄 GameplayTests.cs        # Gameplay scenarios
│   ├── 📄 PerformanceE2E.cs       # Performance E2E
│   └── 📄 StabilityTests.cs       # Stability testing
└── 📁 TestUtilities/              # Test helpers
    ├── 📄 TestBase.cs             # Base test class
    ├── 📄 MockFactory.cs          # Mock objects
    ├── 📄 TestDataBuilder.cs      # Test data builders
    └── 📄 PerformanceAssert.cs    # Performance assertions
```

## Configuration Structure (`config/`)

```
config/
├── 📁 development/                # Development settings
│   ├── 📄 appsettings.Development.json  # App settings
│   ├── 📄 logging.Development.json      # Logging config
│   └── 📄 performance.Development.json  # Performance settings
├── 📁 testing/                    # Testing settings
│   ├── 📄 appsettings.Testing.json     # Test app settings
│   ├── 📄 logging.Testing.json         # Test logging
│   └── 📄 benchmark.Testing.json       # Benchmark config
└── 📁 production/                 # Production settings
    ├── 📄 appsettings.Production.json  # Production settings
    ├── 📄 logging.Production.json      # Production logging
    └── 📄 performance.Production.json  # Production performance
```

## Documentation Structure (`docs/`)

```
docs/
├── 📁 architecture/               # Architecture documentation
│   ├── 📄 system-overview.md     # System overview
│   ├── 📄 component-design.md    # Component design
│   ├── 📄 data-flow.md          # Data flow diagrams
│   └── 📄 deployment.md         # Deployment architecture
├── 📁 api/                       # API documentation
│   ├── 📄 collision-api.md      # Collision API
│   ├── 📄 spatial-api.md        # Spatial API
│   ├── 📄 particle-api.md       # Particle API
│   └── 📄 performance-api.md    # Performance API
├── 📁 guides/                    # Developer guides
│   ├── 📄 getting-started.md    # Getting started
│   ├── 📄 development-guide.md  # Development guide
│   ├── 📄 testing-guide.md      # Testing guide
│   └── 📄 performance-guide.md  # Performance guide
├── 📁 phase2-architecture/       # Phase 2 specifications
│   ├── 📄 PHASE2_SYSTEM_ARCHITECTURE_SPECIFICATION.md
│   ├── 📄 COLLISION_DETECTION_TECHNICAL_SPEC.md
│   ├── 📄 OBJECT_POOLING_TECHNICAL_SPEC.md
│   ├── 📄 SPATIAL_PARTITIONING_TECHNICAL_SPEC.md
│   ├── 📄 ENHANCED_PARTICLE_SYSTEM_TECHNICAL_SPEC.md
│   ├── 📄 PERFORMANCE_MONITORING_TECHNICAL_SPEC.md
│   ├── 📄 ERROR_HANDLING_RESILIENCE_TECHNICAL_SPEC.md
│   ├── 📄 TEST_STRATEGY_COVERAGE_PLAN.md
│   └── 📄 FILE_ORGANIZATION_STRUCTURE.md
└── 📁 diagrams/                  # Architecture diagrams
    ├── 📄 system-architecture.png
    ├── 📄 collision-flow.png
    ├── 📄 spatial-structure.png
    └── 📄 performance-monitoring.png
```

## Project Files

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    
    <!-- Code Analysis -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
    <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory)asteroids.ruleset</CodeAnalysisRuleSet>
    
    <!-- Version Information -->
    <VersionPrefix>2.0.0</VersionPrefix>
    <AssemblyVersion>2.0.0.0</AssemblyVersion>
    <FileVersion>2.0.0.0</FileVersion>
    
    <!-- Package Information -->
    <Authors>Asteroids Development Team</Authors>
    <Company>Asteroids Game Studio</Company>
    <Product>Asteroids Enhanced</Product>
    <Copyright>Copyright © 2024 Asteroids Game Studio</Copyright>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

### Directory.Build.targets

```xml
<Project>
  <!-- Conditional compilation symbols -->
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <DefineConstants>$(DefineConstants);DEBUG_PERFORMANCE;ENABLE_PROFILING</DefineConstants>
  </PropertyGroup>
  
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DefineConstants>$(DefineConstants);RELEASE_OPTIMIZED</DefineConstants>
    <Optimize>true</Optimize>
  </PropertyGroup>

  <!-- Test projects -->
  <PropertyGroup Condition="$(MSBuildProjectName.EndsWith('.Tests'))">
    <IsPackable>false</IsPackable>
    <DefineConstants>$(DefineConstants);TESTING</DefineConstants>
  </PropertyGroup>

  <!-- Benchmark projects -->
  <PropertyGroup Condition="$(MSBuildProjectName.EndsWith('.Benchmarks'))">
    <IsPackable>false</IsPackable>
    <DefineConstants>$(DefineConstants);BENCHMARKING</DefineConstants>
  </PropertyGroup>
</Project>
```

### .editorconfig

```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
indent_style = space
trim_trailing_whitespace = true

[*.cs]
indent_size = 4
max_line_length = 120

# C# Coding Conventions
csharp_prefer_braces = true:suggestion
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_parameter_list_parentheses = false

# .NET Code Quality Rules
dotnet_analyzer_diagnostic.category-performance.severity = warning
dotnet_analyzer_diagnostic.category-security.severity = error
dotnet_analyzer_diagnostic.category-reliability.severity = warning

[*.{json,yml,yaml}]
indent_size = 2

[*.md]
max_line_length = off
trim_trailing_whitespace = false
```

## File Naming Conventions

### C# Files
- **Classes**: `PascalCase` (e.g., `CollisionDetectionSystem.cs`)
- **Interfaces**: `IPascalCase` (e.g., `ICollidable.cs`)
- **Abstract Classes**: `AbstractPascalCase` or `BasePascalCase` (e.g., `BaseParticle.cs`)
- **Enums**: `PascalCase` (e.g., `CollisionLayer.cs`)
- **Static Classes**: `PascalCaseUtils` or `PascalCaseHelper` (e.g., `MathUtils.cs`)

### Test Files
- **Unit Tests**: `{ClassName}Tests.cs` (e.g., `CollisionDetectionSystemTests.cs`)
- **Integration Tests**: `{SystemName}IntegrationTests.cs`
- **Performance Tests**: `{SystemName}PerformanceTests.cs`
- **Benchmark Tests**: `{SystemName}BenchmarkTests.cs`

### Configuration Files
- **Environment-specific**: `{filename}.{Environment}.{extension}`
- **System-specific**: `{systemname}.{Environment}.json`

### Documentation Files
- **Technical Specs**: `UPPERCASE_WITH_UNDERSCORES.md`
- **Guides**: `lowercase-with-hyphens.md`
- **API Docs**: `{api-name}-api.md`

## Namespace Organization

```csharp
// Root namespace
namespace Asteroids

// Core systems
namespace Asteroids.Core
namespace Asteroids.Core.GameEngine
namespace Asteroids.Core.SystemManager
namespace Asteroids.Core.Configuration

// Feature systems
namespace Asteroids.Collision
namespace Asteroids.Collision.BroadPhase
namespace Asteroids.Collision.NarrowPhase
namespace Asteroids.Collision.Resolution

namespace Asteroids.Spatial
namespace Asteroids.Spatial.Grid
namespace Asteroids.Spatial.QuadTree

namespace Asteroids.ObjectPool
namespace Asteroids.ObjectPool.Core
namespace Asteroids.ObjectPool.Specialized

namespace Asteroids.Particles
namespace Asteroids.Particles.Core
namespace Asteroids.Particles.Systems
namespace Asteroids.Particles.Effects

namespace Asteroids.Performance
namespace Asteroids.Performance.Monitoring
namespace Asteroids.Performance.Profiling

namespace Asteroids.Resilience
namespace Asteroids.Resilience.ErrorHandling
namespace Asteroids.Resilience.Recovery

// Game logic
namespace Asteroids.GameLogic
namespace Asteroids.GameLogic.Entities
namespace Asteroids.GameLogic.Components
namespace Asteroids.GameLogic.Systems

// Utilities
namespace Asteroids.Utils
namespace Asteroids.Utils.Mathematics
namespace Asteroids.Utils.Extensions

// 3D implementations
namespace Asteroids.ThreeD
namespace Asteroids.ThreeD.Rendering
namespace Asteroids.ThreeD.Physics

// Testing
namespace Asteroids.Tests
namespace Asteroids.Tests.Unit
namespace Asteroids.Tests.Integration
namespace Asteroids.Tests.Performance
```

This comprehensive file organization structure provides a clear, maintainable, and scalable foundation for the Phase 2 Asteroids game implementation, following industry best practices for C# game development projects.