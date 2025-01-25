using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using MessagePack;
using SharedLibs;
using StarquakeXNA.Particles;

namespace StarquakeXNA
{
    internal class DataDescription
    {
        public string ResourceName;
        public Type ResourceType;
        public string FilePath;

        public DataDescription(string resourceName, string filePath, Type resourceType)
        {
            ResourceName = resourceName;
            FilePath = filePath;
            ResourceType = resourceType;
        }
    }

    internal class SpriteDescription
    {
        public string ResourceName;
        public string FilePath;
        public int Width;
        public int Height;
        public enSpriteEffect SpriteEffect;

        public SpriteDescription(string resourceName, string filePath, int width, int height, enSpriteEffect spriteEffect)
        {
            ResourceName = resourceName;
            FilePath = filePath;
            Width = width;
            Height = height;
            SpriteEffect = spriteEffect;
        }
    }

    internal class AnimSpriteDescription
    {
        public string ResourceName;
        public string FilePath;
        public int Width;
        public int Height;
        public int TileToAnimate;
        public int Delay;

        public AnimSpriteDescription(string resourceName, string filePath, int width, int height, int tileToAnimate, int delay)
        {
            ResourceName = resourceName;
            FilePath = filePath;
            Width = width;
            Height = height;
            TileToAnimate = tileToAnimate;
            Delay = delay;
        }
    }

    public class SoundDescription
    {
        public string ResourceName;
        public string FilePath;
        public enSoundEffectInstanceType InstanceType;
        public int MaxInstances;
        public bool KillOnScreenChange;

        public SoundDescription(string resourceName, string filePath, enSoundEffectInstanceType instanceType, int maxInstances, bool killOnScreenChange)
        {
            ResourceName = resourceName;
            FilePath = filePath;
            InstanceType = instanceType;
            MaxInstances = maxInstances;
            KillOnScreenChange = killOnScreenChange;
        }
    }

    public class ResourceManager : StartableGameComponentBase
    {
        private BackgroundWorker backgroundWorker;

        private List<SpriteDescription> spriteDescriptions;
        private List<DataDescription> dataDescriptions;
        private List<AnimSpriteDescription> animSpriteDescriptions;
        private List<SoundDescription> soundDescriptions;

        private Dictionary<string, object> resources;
        private ParticleEngine particleEngine;

        private AnimTileEngine animationTileEngine;

        // Sound Engine
        private SoundEffectEngine soundEngine;

        public ResourceManager(GameEngine GameEngineInstance) 
            : base(GameEngineInstance)
        {
            particleEngine = new ParticleEngine(gameEngineInstance);
            animationTileEngine = new AnimTileEngine(gameEngineInstance);
            soundEngine = new SoundEffectEngine(gameEngineInstance);
            
            spriteDescriptions = new List<SpriteDescription>();
            animSpriteDescriptions = new List<AnimSpriteDescription>();
            dataDescriptions = new List<DataDescription>();
            soundDescriptions = new List<SoundDescription>();

            resources = new Dictionary<string, object>();
        }

        public void LoadContent()
        {
            resources.Clear();

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);

            UpdateStatus(
               "Loading content....",
               0,
               enGameComponentStatus.Starting);

            backgroundWorker.RunWorkerAsync();
        }

        public void Initialise()
        {
            particleEngine.LoadContent();
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            backgroundWorker.DoWork -= new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);

            backgroundWorker.Dispose();
            backgroundWorker = null;

            UpdateStatus(
                "Launching Game.....",
                100,
                enGameComponentStatus.ContentLoaded);
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            UpdateStatus(
                "Loading content....",
                0,
                enGameComponentStatus.Starting);

            var totalComponentsToLoad =
                spriteDescriptions.Count +
                dataDescriptions.Count +
                animSpriteDescriptions.Count +
                soundDescriptions.Count;

            var componentsLoaded = 0;

            // Sprite Resources
            foreach (var t in spriteDescriptions)
            {
                UpdateStatus(
                    "Loading content, sprite set: '" + t.ResourceName + "'",
                    (int)(double)(((double)componentsLoaded / (double)totalComponentsToLoad) * 100),
                    enGameComponentStatus.Starting);

                SpriteCollection sc =
                    new SpriteCollection(
                        gameEngineInstance,
                        contentManagerInstance,
                        t.FilePath,
                        t.Width,
                        t.Height);

                if (t.SpriteEffect != enSpriteEffect.None)
                {
                    sc.CurrentSpriteEffect = t.SpriteEffect;
                }

                resources.Add(t.ResourceName, sc);
                componentsLoaded++;
            }

            // Anim Sprite Resources
            foreach (var t in animSpriteDescriptions)
            {
                UpdateStatus(
                    "Loading content, anim sprite set: '" + t.ResourceName + "'",
                    (int)(double)(((double)componentsLoaded / (double)totalComponentsToLoad) * 100),
                    enGameComponentStatus.Starting);

                SpriteCollection sc =
                    new SpriteCollection(
                        gameEngineInstance,
                        contentManagerInstance,
                        t.FilePath,
                        t.Width,
                        t.Height);

                animationTileEngine.AnimTilesList.Add(new AnimTile(gameEngineInstance, t.TileToAnimate, sc, t.Delay));
                componentsLoaded++;
            }

            particleEngine.LoadContent();

            // Data Resources
            // TODO this is hacked in right now need to improve
            using (var fs = new FileStream(dataDescriptions[0].FilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the length of the first array
                var lengthBuffer = new byte[sizeof(int)];
                fs.ReadExactly(lengthBuffer, 0, sizeof(int));
                var gameScreensArraySize = BitConverter.ToInt32(lengthBuffer, 0);

                // Read the first array
                var gameScreensData = new byte[gameScreensArraySize];
                fs.ReadExactly(gameScreensData, 0, gameScreensArraySize);

                // Read the length of the second array
                fs.ReadExactly(lengthBuffer, 0, sizeof(int));
                var collisionTilesArraySize = BitConverter.ToInt32(lengthBuffer, 0);

                // Read the second array
                var collisionTilesData = new byte[collisionTilesArraySize];
                fs.ReadExactly(collisionTilesData, 0, collisionTilesArraySize);
                
                resources.Add(dataDescriptions[0].ResourceName, MessagePackSerializer.Deserialize<GameScreen[]>(gameScreensData));
                resources.Add(dataDescriptions[1].ResourceName, MessagePackSerializer.Deserialize<int[,,]>(collisionTilesData));
            }
            
            // Sound resources
            foreach (var t in soundDescriptions)
            {
                UpdateStatus(
                    "Loading content, sound effect: '" + t.ResourceName + "'",
                    (int)(double)(((double)componentsLoaded / (double)totalComponentsToLoad) * 100),
                    enGameComponentStatus.Starting);

                soundEngine.LoadSoundEffect(t);

                componentsLoaded++;
            }

            UpdateStatus(
                "Launching Game.....",
                100,
                enGameComponentStatus.Starting);

            Thread.Sleep(500);
        }

        public void AddSpriteResource(string ResourceName, string FilePath, int SpriteWidth, int SpriteHeight)
        {
            AddSpriteResource(ResourceName, FilePath, SpriteWidth, SpriteHeight, enSpriteEffect.None);
        }

        public void AddSpriteResource(string ResourceName, string FilePath, int SpriteWidth, int SpriteHeight, enSpriteEffect SpriteEffect)
        {
            spriteDescriptions.Add(new SpriteDescription(ResourceName, FilePath, SpriteWidth, SpriteHeight, SpriteEffect));
        }

        public void AddAnimatedSpriteResource(string ResourceName, string FilePath, int SpriteWidth, int SpriteHeight, int TileToAnimate, int Delay)
        {
            animSpriteDescriptions.Add(new AnimSpriteDescription(ResourceName, FilePath, SpriteWidth, SpriteHeight, TileToAnimate, Delay));
        }

        public void AddDataResource(string ResourceName, string FilePath, Type ResourceType)
        {
            dataDescriptions.Add(new DataDescription(ResourceName, FilePath, ResourceType));
        }

        public void AddParticleResource(string EffectName, string EffectPath)
        {
            particleEngine.AddEffect(EffectName, EffectPath);
        }

        public void AddSoundResource(string SoundName, string SoundPath, enSoundEffectInstanceType InstanceType, int MaxInstances, bool KillOnScreenChange)
        {
            soundDescriptions.Add(new SoundDescription(SoundName, SoundPath, InstanceType, MaxInstances, KillOnScreenChange));
        }

        public SpriteCollection GetSpriteCollection(string Name)
        {
            if (resources.ContainsKey(Name))
            {
                return resources[Name] as SpriteCollection;
            }
            return null;
        }

        public IParticleEffect GetParticleEffectInstance(string EffectName, enParticleEffectType EffectType, TimeSpan? Duration)
        {
            if (EffectType == enParticleEffectType.Continual)
            {
                return particleEngine.CreateContinualEffectInstance(EffectName, Duration);
            }
            else if (EffectType == enParticleEffectType.Triggered)
            {
                return particleEngine.CreateSingleTriggerEffectInstance(EffectName);
            }
            else if (EffectType == enParticleEffectType.TriggeredMultiLocationTimed)
            {
                return particleEngine.CreateMultiTriggeredTimeBasedEffectInstance(EffectName);
            }

            return null;
        }

        public AnimTileEngine GetAnimationEngine()
        {
            return animationTileEngine;
        }

        public SoundEffectEngine GetSoundFXEngine()
        {
            return soundEngine;
        }

        public object GetDataResource(string Name)
        {
            if (resources.ContainsKey(Name))
            {
                // Ensure we don't return a sprite array by mistake!
                if (!(resources[Name] is SpriteCollection))
                {
                    return resources[Name] as object;
                }
            }

            return null;
        }
        
    }
}
