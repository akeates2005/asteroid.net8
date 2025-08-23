using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Advanced trail rendering system for projectiles
    /// </summary>
    public class TrailRenderer
    {
        public TrailType TrailType { get; private set; }
        
        private List<TrailSegment> _segments;
        private float _segmentLifespan;
        private int _maxSegments;
        private Random _random;
        
        public TrailRenderer(TrailType trailType)
        {
            TrailType = trailType;
            _segments = new List<TrailSegment>();
            _segmentLifespan = GetSegmentLifespan(trailType);
            _maxSegments = GetMaxSegments(trailType);
            _random = new Random();
        }
        
        private float GetSegmentLifespan(TrailType type)
        {
            return type switch
            {
                TrailType.Electric => 0.3f,
                TrailType.Quantum => 0.8f,
                TrailType.Nanite => 0.5f,
                TrailType.Frost => 0.6f,
                TrailType.Smoke => 1.2f,
                TrailType.Healing => 0.4f,
                TrailType.Plasma => 0.5f,
                TrailType.Gravity => 0.9f,
                TrailType.Dimensional => 1f,
                TrailType.Energy => 0.4f,
                TrailType.Fire => 0.7f,
                TrailType.Metal => 0.8f,
                TrailType.Photon => 0.2f,
                _ => 0.5f
            };
        }
        
        private int GetMaxSegments(TrailType type)
        {
            return type switch
            {
                TrailType.Electric => 8,
                TrailType.Quantum => 15,
                TrailType.Nanite => 20,
                TrailType.Frost => 12,
                TrailType.Smoke => 25,
                TrailType.Healing => 10,
                TrailType.Plasma => 12,
                TrailType.Gravity => 18,
                TrailType.Dimensional => 20,
                TrailType.Energy => 10,
                TrailType.Fire => 15,
                TrailType.Metal => 8,
                TrailType.Photon => 6,
                _ => 10
            };
        }
        
        public void AddPosition(Vector3 position)
        {
            var segment = new TrailSegment(position, _segmentLifespan, GetTime());
            _segments.Insert(0, segment);
            
            // Remove excess segments
            if (_segments.Count > _maxSegments)
            {
                _segments.RemoveAt(_segments.Count - 1);
            }
        }
        
        public void Update(float deltaTime)
        {
            // Update segment lifespans
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                _segments[i].Update(deltaTime);
                if (!_segments[i].IsActive)
                {
                    _segments.RemoveAt(i);
                }
            }
        }
        
        public void Draw(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            if (_segments.Count < 2) return;
            
            switch (TrailType)
            {
                case TrailType.Electric:
                    DrawElectricTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Quantum:
                    DrawQuantumTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Nanite:
                    DrawNaniteTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Frost:
                    DrawFrostTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Smoke:
                    DrawSmokeTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Healing:
                    DrawHealingTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Plasma:
                    DrawPlasmaTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Gravity:
                    DrawGravityTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Dimensional:
                    DrawDimensionalTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Energy:
                    DrawEnergyTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Fire:
                    DrawFireTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Metal:
                    DrawMetalTrail(camera, positions, baseColor);
                    break;
                    
                case TrailType.Photon:
                    DrawPhotonTrail(camera, positions, baseColor);
                    break;
                    
                default:
                    DrawStandardTrail(camera, positions, baseColor);
                    break;
            }
        }
        
        private void DrawElectricTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                Color segmentColor = baseColor;
                segmentColor.A = (byte)(255 * alpha);
                
                // Draw jagged electric line
                Vector3 start = positions[i];
                Vector3 end = positions[i + 1];
                
                // Add electric jitter
                int jitterSegments = 3;
                Vector3 currentPos = start;
                
                for (int j = 0; j < jitterSegments; j++)
                {
                    float t = (float)(j + 1) / jitterSegments;
                    Vector3 targetPos = Vector3.Lerp(start, end, t);
                    
                    // Add random jitter
                    Vector3 jitter = new Vector3(
                        (float)(_random.NextDouble() - 0.5) * 0.5f,
                        (float)(_random.NextDouble() - 0.5) * 0.5f,
                        (float)(_random.NextDouble() - 0.5) * 0.5f
                    );
                    targetPos += jitter;
                    
                    Raylib.DrawLine3D(currentPos, targetPos, segmentColor);
                    currentPos = targetPos;
                }
                
                // Draw electric sparks
                if (_random.NextSingle() < 0.3f)
                {
                    Vector3 sparkPos = Vector3.Lerp(start, end, (float)_random.NextDouble());
                    Vector3 sparkDir = new Vector3(
                        (float)(_random.NextDouble() - 0.5),
                        (float)(_random.NextDouble() - 0.5),
                        (float)(_random.NextDouble() - 0.5)
                    ) * 0.8f;
                    
                    Raylib.DrawLine3D(sparkPos, sparkPos + sparkDir, Color.White);
                }
            }
        }
        
        private void DrawQuantumTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            float time = GetTime();
            
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                
                // Draw multiple quantum states
                for (int state = 0; state < 3; state++)
                {
                    Color stateColor = ColorLerp(baseColor, Color.Purple, (float)state / 2f);
                    stateColor.A = (byte)(255 * alpha / (state + 1));
                    
                    Vector3 start = positions[i];
                    Vector3 end = positions[i + 1];
                    
                    // Add quantum uncertainty
                    float stateOffset = state * MathF.PI * 2f / 3f;
                    Vector3 uncertainty = new Vector3(
                        MathF.Sin(time * 4f + stateOffset) * 0.3f,
                        MathF.Cos(time * 3f + stateOffset) * 0.3f,
                        MathF.Sin(time * 2f + stateOffset) * 0.3f
                    );
                    
                    Raylib.DrawLine3D(start + uncertainty, end + uncertainty, stateColor);
                }
            }
        }
        
        private void DrawNaniteTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            // Draw swarm of small particles
            for (int i = 0; i < positions.Count; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                Color naniteColor = baseColor;
                naniteColor.A = (byte)(255 * alpha);
                
                // Draw multiple nanites around each position
                for (int j = 0; j < 3; j++)
                {
                    Vector3 offset = new Vector3(
                        (float)(_random.NextDouble() - 0.5) * 0.5f,
                        (float)(_random.NextDouble() - 0.5) * 0.5f,
                        (float)(_random.NextDouble() - 0.5) * 0.5f
                    );
                    
                    Raylib.DrawSphere(positions[i] + offset, 0.05f, naniteColor);
                }
            }
        }
        
        private void DrawFrostTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                Color frostColor = ColorLerp(baseColor, Color.White, 0.5f);
                frostColor.A = (byte)(255 * alpha);
                
                Raylib.DrawLine3D(positions[i], positions[i + 1], frostColor);
                
                // Draw ice crystals
                if (i % 2 == 0)
                {
                    Vector3 crystalPos = Vector3.Lerp(positions[i], positions[i + 1], 0.5f);
                    Raylib.DrawSphere(crystalPos, 0.1f * alpha, Color.LightBlue);
                }
            }
        }
        
        private void DrawSmokeTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                float size = 0.3f * (1f + i * 0.1f); // Smoke expands
                
                Color smokeColor = ColorLerp(baseColor, Color.Gray, (float)i / positions.Count);
                smokeColor.A = (byte)(100 * alpha); // Semi-transparent
                
                Raylib.DrawSphere(positions[i], size, smokeColor);
            }
        }
        
        private void DrawHealingTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            float time = GetTime();
            
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                Color healingColor = ColorLerp(baseColor, Color.White, MathF.Sin(time * 6f) * 0.3f + 0.3f);
                healingColor.A = (byte)(255 * alpha);
                
                Raylib.DrawLine3D(positions[i], positions[i + 1], healingColor);
                
                // Draw healing sparkles
                if (_random.NextSingle() < 0.4f)
                {
                    Vector3 sparklePos = Vector3.Lerp(positions[i], positions[i + 1], (float)_random.NextDouble());
                    sparklePos += new Vector3(0, 0.2f, 0); // Float upward
                    Raylib.DrawSphere(sparklePos, 0.08f, Color.White);
                }
            }
        }
        
        private void DrawPlasmaTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                Color plasmaColor = baseColor;
                plasmaColor.A = (byte)(255 * alpha);
                
                // Draw main trail
                Raylib.DrawLine3D(positions[i], positions[i + 1], plasmaColor);
                
                // Draw plasma glow
                Color glowColor = plasmaColor;
                glowColor.A = (byte)(glowColor.A / 3);
                
                Vector3 midPoint = Vector3.Lerp(positions[i], positions[i + 1], 0.5f);
                Raylib.DrawSphere(midPoint, 0.2f * alpha, glowColor);
            }
        }
        
        private void DrawGravityTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            float time = GetTime();
            
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                Color gravityColor = baseColor;
                gravityColor.A = (byte)(255 * alpha);
                
                // Draw distorted space effect
                Vector3 start = positions[i];
                Vector3 end = positions[i + 1];
                
                // Add gravitational lensing effect
                Vector3 warp = new Vector3(
                    MathF.Sin(time * 2f + i * 0.5f) * 0.2f,
                    MathF.Cos(time * 1.5f + i * 0.3f) * 0.2f,
                    MathF.Sin(time * 2.5f + i * 0.4f) * 0.2f
                );
                
                Raylib.DrawLine3D(start + warp, end + warp, gravityColor);
                
                // Draw gravity wells
                if (i % 3 == 0)
                {
                    Vector3 wellPos = Vector3.Lerp(start, end, 0.5f);
                    Raylib.DrawSphereWires(wellPos, 0.3f * alpha, Color.Purple);
                }
            }
        }
        
        private void DrawDimensionalTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            float time = GetTime();
            
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                
                // Draw trail phasing in and out of dimensions
                float phaseValue = MathF.Sin(time * 3f + i * 0.8f);
                if (phaseValue > -0.5f) // Only draw when "in phase"
                {
                    Color dimColor = ColorLerp(baseColor, Color.Magenta, Math.Abs(phaseValue));
                    dimColor.A = (byte)(255 * alpha * Math.Abs(phaseValue));
                    
                    Vector3 start = positions[i];
                    Vector3 end = positions[i + 1];
                    
                    // Add dimensional distortion
                    Vector3 distortion = new Vector3(
                        MathF.Sin(time * 4f + i) * 0.4f,
                        MathF.Cos(time * 3f + i) * 0.4f,
                        MathF.Sin(time * 2f + i) * 0.4f
                    ) * phaseValue;
                    
                    Raylib.DrawLine3D(start + distortion, end + distortion, dimColor);
                }
            }
        }
        
        private void DrawEnergyTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            float time = GetTime();
            
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                float pulse = MathF.Sin(time * 8f - i * 0.5f) * 0.3f + 0.7f;
                
                Color energyColor = baseColor;
                energyColor.A = (byte)(255 * alpha * pulse);
                
                Raylib.DrawLine3D(positions[i], positions[i + 1], energyColor);
                
                // Draw energy ripples
                Vector3 midPoint = Vector3.Lerp(positions[i], positions[i + 1], 0.5f);
                Raylib.DrawSphereWires(midPoint, 0.15f * pulse, energyColor);
            }
        }
        
        private void DrawFireTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                float fireAge = (float)i / positions.Count;
                
                // Fire changes from yellow to red over time
                Color fireColor = ColorLerp(Color.Yellow, Color.Red, fireAge);
                fireColor.A = (byte)(255 * alpha);
                
                // Add flickering
                float flicker = 1f + MathF.Sin(GetTime() * 15f + i) * 0.2f;
                float size = 0.15f * alpha * flicker;
                
                Raylib.DrawSphere(positions[i], size, fireColor);
                
                // Draw rising embers
                if (_random.NextSingle() < 0.2f)
                {
                    Vector3 emberPos = positions[i] + new Vector3(0, 0.3f, 0);
                    Raylib.DrawSphere(emberPos, 0.05f, Color.Orange);
                }
            }
        }
        
        private void DrawMetalTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                Color metalColor = ColorLerp(baseColor, Color.Silver, 0.5f);
                metalColor.A = (byte)(255 * alpha);
                
                Raylib.DrawLine3D(positions[i], positions[i + 1], metalColor);
                
                // Draw metal fragments
                if (i % 2 == 0)
                {
                    Vector3 fragmentPos = Vector3.Lerp(positions[i], positions[i + 1], 0.5f);
                    Raylib.DrawCube(fragmentPos, 0.1f, 0.05f, 0.1f, metalColor);
                }
            }
        }
        
        private void DrawPhotonTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            // Photons leave brief, bright trails
            for (int i = 0; i < Math.Min(positions.Count - 1, 3); i++) // Only last few positions
            {
                float alpha = 1f - ((float)i / 3f);
                Color photonColor = Color.White;
                photonColor.A = (byte)(255 * alpha);
                
                Raylib.DrawLine3D(positions[i], positions[i + 1], photonColor);
                
                // Draw light bloom
                Vector3 midPoint = Vector3.Lerp(positions[i], positions[i + 1], 0.5f);
                Raylib.DrawSphere(midPoint, 0.1f * alpha, photonColor);
            }
        }
        
        private void DrawStandardTrail(Camera3D camera, List<Vector3> positions, Color baseColor)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / positions.Count);
                Color trailColor = baseColor;
                trailColor.A = (byte)(255 * alpha);
                
                Raylib.DrawLine3D(positions[i], positions[i + 1], trailColor);
            }
        }
        
        private Color ColorLerp(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Color(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t),
                (byte)(a.A + (b.A - a.A) * t)
            );
        }
        
        private float GetTime() => (float)Raylib.GetTime();
    }
    
    /// <summary>
    /// Individual trail segment with lifetime tracking
    /// </summary>
    public class TrailSegment
    {
        public Vector3 Position { get; private set; }
        public float Lifespan { get; private set; }
        public float CreationTime { get; private set; }
        public bool IsActive { get; private set; }
        
        private float _maxLifespan;
        
        public TrailSegment(Vector3 position, float lifespan, float creationTime)
        {
            Position = position;
            Lifespan = lifespan;
            _maxLifespan = lifespan;
            CreationTime = creationTime;
            IsActive = true;
        }
        
        public void Update(float deltaTime)
        {
            if (!IsActive) return;
            
            Lifespan -= deltaTime;
            if (Lifespan <= 0)
            {
                IsActive = false;
            }
        }
        
        public float GetAlpha()
        {
            if (!IsActive) return 0f;
            return Lifespan / _maxLifespan;
        }
    }
}