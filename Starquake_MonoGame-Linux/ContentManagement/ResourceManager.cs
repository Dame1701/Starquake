using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Threading;
using StarquakeXNA.Particles;

namespace StarquakeXNA
{
    public enum enResourceType
    {
        Sprite,
        ParticleEffect
    }

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
        private StarquakeXNA.Particles.ParticleEngine particleEngine;

        private AnimTileEngine animationTileEngine;

        // Sound Engine
        private SoundEffectEngine soundEngine;

        public ResourceManager(GameEngine GameEngineInstance) 
            : base(GameEngineInstance)
        {
            particleEngine = new StarquakeXNA.Particles.ParticleEngine(gameEngineInstance);
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

            int totalComponentsToLoad =
                spriteDescriptions.Count +
                dataDescriptions.Count +
                animSpriteDescriptions.Count +
                soundDescriptions.Count;

            int componentsLoaded = 0;

            // Sprite Resources
            for (int a = 0; a < spriteDescriptions.Count; a++)
            {
                UpdateStatus(
                    "Loading content, sprite set: '" + spriteDescriptions[a].ResourceName + "'",
                    (int)(double)(((double)componentsLoaded / (double)totalComponentsToLoad) * 100),
                    enGameComponentStatus.Starting);

                SpriteCollection sc =
                    new SpriteCollection(
                        gameEngineInstance,
                        contentManagerInstance,
                        spriteDescriptions[a].FilePath,
                        spriteDescriptions[a].Width,
                        spriteDescriptions[a].Height);

                if (spriteDescriptions[a].SpriteEffect != enSpriteEffect.None)
                {
                    sc.CurrentSpriteEffect = spriteDescriptions[a].SpriteEffect;
                }

                resources.Add(spriteDescriptions[a].ResourceName, sc);
                componentsLoaded++;
            }

            // Anim Sprite Resources
            for (int a = 0; a < animSpriteDescriptions.Count; a++)
            {
                UpdateStatus(
                    "Loading content, anim sprite set: '" + animSpriteDescriptions[a].ResourceName + "'",
                    (int)(double)(((double)componentsLoaded / (double)totalComponentsToLoad) * 100),
                    enGameComponentStatus.Starting);

                SpriteCollection sc =
                    new SpriteCollection(
                        gameEngineInstance,
                        contentManagerInstance,
                        animSpriteDescriptions[a].FilePath,
                        animSpriteDescriptions[a].Width,
                        animSpriteDescriptions[a].Height);

                animationTileEngine.AnimTilesList.Add(new AnimTile(gameEngineInstance, animSpriteDescriptions[a].TileToAnimate, sc, animSpriteDescriptions[a].Delay));
                componentsLoaded++;
            }

            particleEngine.LoadContent();

            // Data Resources
            for (int a = 0; a < dataDescriptions.Count; a++)
            {
                UpdateStatus(
                    "Loading content, data: '" + dataDescriptions[a].ResourceName + "'",
                    (int)(double)(((double)componentsLoaded / (double)totalComponentsToLoad) * 100),
                    enGameComponentStatus.Starting);

                using (FileStream dataReader = new FileStream(dataDescriptions[a].FilePath, FileMode.Open))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Binder = new SharedLibs.StarquakeDeserializationBinder();

                    object o = binaryFormatter.Deserialize(dataReader);
                    resources.Add(dataDescriptions[a].ResourceName, o);
                    componentsLoaded++;

                    // Grab anything else in the same data array
                    for (int b = a + 1; b < dataDescriptions.Count; b++)
                    {
                        if (dataDescriptions[b].FilePath == dataDescriptions[a].FilePath)
                        {
                            UpdateStatus(
                                "Loading content, data: '" + dataDescriptions[b].ResourceName + "'",
                                (int)(double)(((double)componentsLoaded / (double)totalComponentsToLoad) * 100),
                                enGameComponentStatus.Starting);

                            o = binaryFormatter.Deserialize(dataReader);
                            resources.Add(dataDescriptions[b].ResourceName, o);
                            componentsLoaded++;
                            a++;
                        }
                    }
                }
            }

            // Sound resources
            for (int a = 0; a < soundDescriptions.Count; a++)
            {
                UpdateStatus(
                    "Loading content, sound effect: '" + soundDescriptions[a].ResourceName + "'",
                    (int)(double)(((double)componentsLoaded / (double)totalComponentsToLoad) * 100),
                    enGameComponentStatus.Starting);

                soundEngine.LoadSoundEffect(soundDescriptions[a]);

                componentsLoaded++;
            }

            UpdateStatus(
                "Launching Game.....",
                100,
                enGameComponentStatus.Starting);

            System.Threading.Thread.Sleep(500);
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
