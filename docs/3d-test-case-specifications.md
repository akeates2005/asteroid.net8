# 3D Test Case Specifications

## Overview
This document provides detailed test case specifications for the 3D Asteroids implementation, organized by test category and priority.

## Unit Test Specifications

### UT-001: Vector3 Mathematics
**Category**: Unit Test  
**Priority**: Critical  
**Component**: Physics3D

#### Test Cases:

**UT-001.1: Vector3 Distance Calculation**
```csharp
[Test]
public void Vector3_Distance_3D_Pythagorean()
{
    // Test 3-4-5 triangle in 3D: (0,0,0) to (3,4,0) = 5
    var result = Vector3.Distance(Vector3.Zero, new Vector3(3, 4, 0));
    Assert.AreEqual(5.0f, result, 0.001f);
}

[Test] 
public void Vector3_Distance_3D_Complex()
{
    // Test 3-4-12 triangle: (0,0,0) to (3,4,12) = 13
    var result = Vector3.Distance(Vector3.Zero, new Vector3(3, 4, 12));
    Assert.AreEqual(13.0f, result, 0.001f);
}
```

**UT-001.2: Vector3 Normalization**
```csharp
[Test]
public void Vector3_Normalization_Unit_Vector()
{
    var vector = new Vector3(3, 4, 0);
    var normalized = Vector3.Normalize(vector);
    var expectedLength = 1.0f;
    
    Assert.AreEqual(expectedLength, normalized.Length(), 0.001f);
    Assert.AreEqual(0.6f, normalized.X, 0.001f);
    Assert.AreEqual(0.8f, normalized.Y, 0.001f);
}
```

### UT-002: 3D Collision Detection
**Category**: Unit Test  
**Priority**: Critical  
**Component**: CollisionManager3D

#### Test Cases:

**UT-002.1: Sphere-Sphere Collision Detection**
```csharp
[Test]
public void SphereCollision_Should_Detect_Overlap()
{
    var pos1 = Vector3.Zero;
    var pos2 = new Vector3(8, 0, 0);
    var radius1 = 5f;
    var radius2 = 5f;
    
    var result = CollisionManager3D.CheckSphereCollision(pos1, radius1, pos2, radius2);
    Assert.IsTrue(result, "Spheres should overlap when distance < sum of radii");
}

[Test]
public void SphereCollision_Should_Not_Detect_Separation()
{
    var pos1 = Vector3.Zero;
    var pos2 = new Vector3(12, 0, 0);
    var radius1 = 5f;
    var radius2 = 5f;
    
    var result = CollisionManager3D.CheckSphereCollision(pos1, radius1, pos2, radius2);
    Assert.IsFalse(result, "Spheres should not overlap when distance > sum of radii");
}
```

**UT-002.2: Edge Case Testing**
```csharp
[Test]
public void SphereCollision_Exact_Touch()
{
    var pos1 = Vector3.Zero;
    var pos2 = new Vector3(10, 0, 0);
    var radius1 = 5f;
    var radius2 = 5f;
    
    var result = CollisionManager3D.CheckSphereCollision(pos1, radius1, pos2, radius2);
    Assert.IsTrue(result, "Spheres should collide when exactly touching");
}

[Test]
public void SphereCollision_Zero_Radius()
{
    var pos1 = Vector3.Zero;
    var pos2 = Vector3.Zero;
    var radius1 = 0f;
    var radius2 = 5f;
    
    var result = CollisionManager3D.CheckSphereCollision(pos1, radius1, pos2, radius2);
    Assert.IsTrue(result, "Point collision should be detected");
}
```

### UT-003: Matrix Transformations
**Category**: Unit Test  
**Priority**: High  
**Component**: TransformManager3D

#### Test Cases:

**UT-003.1: Translation Matrix**
```csharp
[Test]
public void Matrix_Translation_Transforms_Point()
{
    var translation = new Vector3(10, 20, 30);
    var matrix = Matrix4x4.CreateTranslation(translation);
    var point = Vector3.Zero;
    
    var result = Vector3.Transform(point, matrix);
    
    Assert.AreEqual(translation.X, result.X, 0.001f);
    Assert.AreEqual(translation.Y, result.Y, 0.001f);
    Assert.AreEqual(translation.Z, result.Z, 0.001f);
}
```

**UT-003.2: Rotation Matrix**
```csharp
[Test]
public void Matrix_Rotation_Y_90Degrees()
{
    var matrix = Matrix4x4.CreateRotationY(MathF.PI / 2);
    var vector = Vector3.UnitX; // (1, 0, 0)
    
    var result = Vector3.Transform(vector, matrix);
    var expected = new Vector3(0, 0, -1);
    
    AssertVector3Equal(expected, result, 0.001f);
}
```

### UT-004: Camera System
**Category**: Unit Test  
**Priority**: High  
**Component**: Camera3D

#### Test Cases:

**UT-004.1: Camera Initialization**
```csharp
[Test]
public void Camera_Initialization_Default_Values()
{
    var camera = new Camera3D
    {
        Position = new Vector3(0, 20, 20),
        Target = Vector3.Zero,
        Up = Vector3.UnitY,
        FovY = 60f,
        Projection = CameraProjection.Perspective
    };
    
    Assert.AreEqual(new Vector3(0, 20, 20), camera.Position);
    Assert.AreEqual(Vector3.Zero, camera.Target);
    Assert.AreEqual(Vector3.UnitY, camera.Up);
    Assert.AreEqual(60f, camera.FovY);
}
```

## Integration Test Specifications

### IT-001: Game Manager 3D Integration
**Category**: Integration Test  
**Priority**: Critical  
**Component**: GameProgram + Renderer3D

#### Test Cases:

**IT-001.1: 3D Mode Switching**
```csharp
[Test]
public void GameManager_Toggle3D_Preserves_State()
{
    // Arrange
    var gameManager = new GameProgram();
    gameManager.Initialize();
    var initialScore = gameManager.Score;
    var initialLevel = gameManager.Level;
    
    // Act
    var renderer = gameManager.GetRenderer();
    var is3D = renderer.Toggle3DMode();
    
    // Assert
    Assert.IsTrue(is3D || renderer is Renderer3D, "Should activate 3D mode");
    Assert.AreEqual(initialScore, gameManager.Score);
    Assert.AreEqual(initialLevel, gameManager.Level);
}
```

**IT-001.2: Game Loop Integration**
```csharp
[Test]
public void GameManager_3D_UpdateLoop_Stable()
{
    // Arrange
    var gameManager = new GameProgram();
    gameManager.Initialize();
    var renderer = gameManager.GetRenderer();
    if (!renderer.Is3DModeActive) renderer.Toggle3DMode();
    
    // Act & Assert - Run multiple update cycles
    for (int i = 0; i < 100; i++)
    {
        Assert.DoesNotThrow(() => gameManager.Update());
        Assert.IsTrue(gameManager.IsGameRunning);
    }
}
```

### IT-002: Renderer Interface Integration
**Category**: Integration Test  
**Priority**: Critical  
**Component**: IRenderer + Renderer3D

#### Test Cases:

**IT-002.1: All Rendering Methods Work**
```csharp
[Test]
public void Renderer3D_All_Methods_Functional()
{
    // Arrange
    var renderer = new Renderer3D();
    Assert.IsTrue(renderer.Initialize());
    
    // Act & Assert
    renderer.BeginFrame();
    
    Assert.DoesNotThrow(() => renderer.RenderPlayer(
        new Vector2(100, 100), 0f, Color.White, false));
        
    Assert.DoesNotThrow(() => renderer.RenderAsteroid(
        new Vector2(200, 200), 30f, Color.Gray, 12345));
        
    Assert.DoesNotThrow(() => renderer.RenderBullet(
        new Vector2(150, 150), Color.Yellow));
        
    Assert.DoesNotThrow(() => renderer.RenderExplosion(
        new Vector2(175, 175), 0.8f, Color.Orange));
        
    renderer.EndFrame();
}
```

### IT-003: Spatial Grid Integration
**Category**: Integration Test  
**Priority**: High  
**Component**: SpatialGrid + CollisionManager3D

#### Test Cases:

**IT-003.1: Spatial Partitioning Performance**
```csharp
[Test]
public void SpatialGrid_3D_Collision_Performance()
{
    // Arrange
    var spatialGrid = new SpatialGrid(50f);
    var entities = CreateTest3DEntities(100);
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    foreach (var entity in entities)
    {
        spatialGrid.Insert(entity);
    }
    
    var collisions = spatialGrid.Query(Vector3.Zero, 25f);
    stopwatch.Stop();
    
    // Assert
    Assert.Less(stopwatch.ElapsedMilliseconds, 10);
    Assert.Greater(collisions.Count, 0);
}
```

## Performance Test Specifications

### PT-001: 3D Rendering Performance
**Category**: Performance Test  
**Priority**: Critical  
**Component**: Renderer3D

#### Test Cases:

**PT-001.1: Frame Rate Benchmark**
```csharp
[Test]
[Category("Performance")]
public void Renderer3D_Maintains_60FPS_With_100_Objects()
{
    // Arrange
    var renderer = new Renderer3D();
    renderer.Initialize();
    var gameObjects = CreateTestScene(100);
    
    // Act
    var frameCount = 60; // 1 second of frames
    var stopwatch = Stopwatch.StartNew();
    
    for (int frame = 0; frame < frameCount; frame++)
    {
        renderer.BeginFrame();
        foreach (var obj in gameObjects)
        {
            obj.Render(renderer);
        }
        renderer.EndFrame();
    }
    
    stopwatch.Stop();
    
    // Assert
    var avgFrameTime = (double)stopwatch.ElapsedMilliseconds / frameCount;
    var targetFrameTime = 1000.0 / 60.0; // 16.67ms for 60fps
    
    Assert.LessOrEqual(avgFrameTime, targetFrameTime,
        $"Average frame time {avgFrameTime:F2}ms exceeds 60fps requirement");
}
```

**PT-001.2: Memory Usage Benchmark**
```csharp
[Test]
[Category("Performance")]
public void Renderer3D_Memory_Usage_Acceptable()
{
    // Arrange
    GC.Collect();
    var initialMemory = GC.GetTotalMemory(true);
    
    // Act
    var renderer = new Renderer3D();
    renderer.Initialize();
    
    // Simulate 1000 frames of rendering
    for (int frame = 0; frame < 1000; frame++)
    {
        renderer.BeginFrame();
        RenderTestScene(renderer);
        renderer.EndFrame();
    }
    
    GC.Collect();
    var finalMemory = GC.GetTotalMemory(true);
    
    // Assert
    var memoryIncrease = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB
    Assert.Less(memoryIncrease, 50.0,
        $"Memory increase of {memoryIncrease:F2}MB is too high");
}
```

### PT-002: Collision Performance
**Category**: Performance Test  
**Priority**: High  
**Component**: CollisionManager3D

#### Test Cases:

**PT-002.1: Collision Detection Throughput**
```csharp
[Test]
[Category("Performance")]
public void Collision3D_Throughput_Exceeds_100K_Per_Second()
{
    // Arrange
    var testPairs = GenerateCollisionTestPairs(10000);
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    foreach (var (pos1, r1, pos2, r2) in testPairs)
    {
        CollisionManager3D.CheckSphereCollision(pos1, r1, pos2, r2);
    }
    stopwatch.Stop();
    
    // Assert
    var operationsPerSecond = testPairs.Count / (stopwatch.ElapsedMilliseconds / 1000.0);
    Assert.GreaterOrEqual(operationsPerSecond, 100000,
        $"Collision throughput {operationsPerSecond:F0} ops/sec is below requirement");
}
```

## Regression Test Specifications

### RT-001: Existing Functionality Preservation
**Category**: Regression Test  
**Priority**: Critical  
**Component**: All existing systems

#### Test Cases:

**RT-001.1: Classic 2D Gameplay Unchanged**
```csharp
[Test]
[Category("Regression")]
public void Classic_2D_Mode_Unchanged()
{
    // Arrange
    var gameManager = new GameProgram();
    gameManager.Initialize();
    var renderer = gameManager.GetRenderer();
    
    // Ensure 2D mode
    if (renderer.Is3DModeActive)
        renderer.Toggle3DMode();
    
    // Act & Assert - Test all classic behaviors
    var player = gameManager.Player;
    var initialPos = player.Position;
    
    // Player movement
    SimulateKeyPress(KeyboardKey.Up);
    gameManager.Update();
    Assert.AreNotEqual(initialPos, player.Position, "Player should move");
    
    // Bullet firing
    var initialBulletCount = gameManager.GetBulletCount();
    SimulateKeyPress(KeyboardKey.Space);
    gameManager.Update();
    Assert.Greater(gameManager.GetBulletCount(), initialBulletCount, 
        "Should fire bullets");
    
    // Asteroid collision
    var asteroid = gameManager.GetFirstAsteroid();
    if (TestBulletAsteroidCollision(gameManager))
    {
        var initialScore = gameManager.Score;
        gameManager.Update();
        Assert.Greater(gameManager.Score, initialScore, "Score should increase");
    }
}
```

**RT-001.2: Phase 1 Validation Still Passes**
```csharp
[Test]
[Category("Regression")]
public void Phase1_Tests_Still_Pass()
{
    // Run the existing comprehensive test suite
    var testSuite = new Phase1_ComprehensiveTestSuite();
    var results = testSuite.RunAllTests();
    
    Assert.AreEqual(0, results.FailedTests, 
        "No Phase 1 tests should fail after 3D implementation");
    Assert.AreEqual(100.0, results.SuccessRate, 0.1,
        "Success rate should remain 100%");
}
```

## End-to-End Test Specifications

### E2E-001: Complete 3D Gameplay
**Category**: End-to-End Test  
**Priority**: Critical  
**Component**: Complete Game System

#### Test Cases:

**E2E-001.1: Full 3D Game Session**
```csharp
[Test]
[Category("EndToEnd")]
public void Complete_3D_Game_Session()
{
    // Arrange
    var gameManager = new GameProgram();
    gameManager.Initialize();
    var renderer = gameManager.GetRenderer();
    renderer.Toggle3DMode();
    
    // Assert initial state
    Assert.IsTrue(renderer.Is3DModeActive, "Should be in 3D mode");
    Assert.IsNotNull(gameManager.Player, "Player should exist");
    Assert.Greater(gameManager.Asteroids.Count, 0, "Should have asteroids");
    
    // Act & Assert - Complete gameplay flow
    
    // 1. Player movement in 3D
    var initialPlayerPos = gameManager.Player.Position;
    SimulateInput(KeyboardKey.Up, KeyboardKey.Right);
    gameManager.Update();
    Assert.AreNotEqual(initialPlayerPos, gameManager.Player.Position,
        "Player should move in 3D space");
    
    // 2. 3D bullet mechanics
    var initialBulletCount = gameManager.GetBulletCount();
    SimulateInput(KeyboardKey.Space);
    gameManager.Update();
    Assert.Greater(gameManager.GetBulletCount(), initialBulletCount,
        "Should fire bullets in 3D");
    
    // 3. 3D collision detection
    var initialScore = gameManager.Score;
    if (SimulateBulletAsteroidCollision(gameManager))
    {
        Assert.Greater(gameManager.Score, initialScore,
            "3D collisions should register");
    }
    
    // 4. 3D camera system
    var cameraState = renderer.GetCameraState();
    Assert.IsTrue(cameraState.IsActive, "3D camera should be active");
    Assert.AreNotEqual(Vector3.Zero, cameraState.Position,
        "Camera should have 3D position");
    
    // 5. Level progression in 3D
    var initialLevel = gameManager.Level;
    DestroyAllAsteroids(gameManager);
    gameManager.Update();
    Assert.GreaterOrEqual(gameManager.Level, initialLevel,
        "Should handle level progression in 3D");
}
```

### E2E-002: Mode Switching Scenarios
**Category**: End-to-End Test  
**Priority**: High  
**Component**: Complete Game System

#### Test Cases:

**E2E-002.1: Seamless 2D/3D Switching**
```csharp
[Test]
[Category("EndToEnd")]
public void Seamless_2D_3D_Mode_Switching()
{
    // Arrange
    var gameManager = new GameProgram();
    gameManager.Initialize();
    var renderer = gameManager.GetRenderer();
    
    // Start in 2D, capture state
    var score2D = gameManager.Score;
    var playerPos2D = gameManager.Player.Position;
    var asteroidCount2D = gameManager.Asteroids.Count;
    
    // Switch to 3D
    renderer.Toggle3DMode();
    Assert.IsTrue(renderer.Is3DModeActive, "Should switch to 3D");
    
    // Verify state preservation
    Assert.AreEqual(score2D, gameManager.Score, "Score preserved in 3D");
    Assert.AreEqual(playerPos2D, gameManager.Player.Position, "Position preserved");
    Assert.AreEqual(asteroidCount2D, gameManager.Asteroids.Count, "Asteroids preserved");
    
    // Play in 3D mode
    for (int i = 0; i < 10; i++)
    {
        gameManager.Update();
    }
    
    var score3D = gameManager.Score;
    var playerPos3D = gameManager.Player.Position;
    var asteroidCount3D = gameManager.Asteroids.Count;
    
    // Switch back to 2D
    renderer.Toggle3DMode();
    Assert.IsFalse(renderer.Is3DModeActive, "Should switch back to 2D");
    
    // Verify state preservation
    Assert.AreEqual(score3D, gameManager.Score, "Score preserved back to 2D");
    Assert.AreEqual(asteroidCount3D, gameManager.Asteroids.Count, "Game state preserved");
    
    // Continue playing in 2D
    for (int i = 0; i < 10; i++)
    {
        gameManager.Update();
    }
    
    Assert.IsTrue(gameManager.IsGameRunning, "Game should continue smoothly");
}
```

## Test Utilities and Helpers

### Helper Methods
```csharp
public static class TestHelpers
{
    public static void AssertVector3Equal(Vector3 expected, Vector3 actual, float tolerance = 0.001f)
    {
        var distance = Vector3.Distance(expected, actual);
        Assert.LessOrEqual(distance, tolerance,
            $"Vector3 mismatch: Expected {expected}, Actual {actual}, Distance {distance}");
    }
    
    public static List<GameObject3D> CreateTestScene(int objectCount)
    {
        var objects = new List<GameObject3D>();
        var random = new Random(42); // Fixed seed for reproducible tests
        
        for (int i = 0; i < objectCount; i++)
        {
            objects.Add(new Asteroid3D(
                new Vector3(random.Next(800), random.Next(600), random.Next(100)),
                Vector3.One,
                AsteroidSize.Medium,
                random,
                1));
        }
        
        return objects;
    }
    
    public static void SimulateInput(params KeyboardKey[] keys)
    {
        // Mock input simulation for testing
        foreach (var key in keys)
        {
            InputManager.SimulateKeyPress(key);
        }
    }
}
```

## Test Execution Strategy

### Test Categories Execution Order
1. **Unit Tests** - Run first, must pass 100%
2. **Integration Tests** - Run second, core system validation
3. **Performance Tests** - Run third, benchmark validation
4. **Regression Tests** - Run fourth, existing functionality protection
5. **End-to-End Tests** - Run last, complete system validation

### Continuous Integration Rules
- All Unit and Integration tests must pass for merge
- Performance tests must meet benchmarks
- Regression tests must pass 100%
- End-to-end tests must pass for release

### Test Data Management
- Use fixed seeds for reproducible randomization
- Create reusable test data builders
- Clean up resources after each test
- Use dependency injection for external dependencies

This comprehensive test specification ensures thorough validation of the 3D implementation while maintaining the integrity of existing functionality.