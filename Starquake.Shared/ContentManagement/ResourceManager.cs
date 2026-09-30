// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Content;
using MessagePack;
using Starquake.Animations;
using Starquake.Engines;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ContentManagement;

public class ResourceManager : StartableGameComponentBase
{
    /// <summary>
    ///     The percentage reported once everything has loaded, and how long the "Launching Game" message
    ///     is left up before the game takes over - long enough to be read rather than flash past.
    /// </summary>
    private const int FullyLoadedPercent = 100;

    /// <inheritdoc cref="FullyLoadedPercent" />
    private const int LaunchPauseMs = 500;

    private readonly AnimTileEngine _animationTileEngine;
    private readonly ContentManager _contentManagerInstance;
    private readonly GameEngine _gameEngineInstance;
    private readonly List<AnimSpriteDescription> _animSpriteDescriptions;
    private readonly List<DataDescription> _dataDescriptions;
    private readonly ParticleEngine _particleEngine;

    private readonly Dictionary<string, object> _resources;
    private readonly List<SoundDescription> _soundDescriptions;

    // Sound Engine
    private readonly SoundEffectEngine _soundEngine;

    private readonly List<SpriteDescription> _spriteDescriptions;

    public ResourceManager(GameEngine gameEngineInstance)
    {
        _gameEngineInstance = gameEngineInstance;
        _contentManagerInstance = gameEngineInstance.ContentMgr;

        _particleEngine = new ParticleEngine(_gameEngineInstance);
        _animationTileEngine = new AnimTileEngine(_gameEngineInstance);
        _soundEngine = new SoundEffectEngine(_gameEngineInstance);

        _spriteDescriptions = new List<SpriteDescription>();
        _animSpriteDescriptions = new List<AnimSpriteDescription>();
        _dataDescriptions = new List<DataDescription>();
        _soundDescriptions = new List<SoundDescription>();

        _resources = new Dictionary<string, object>();
    }

    public void LoadContent()
    {
        _resources.Clear();

        var backgroundWorker = new BackgroundWorker();
        backgroundWorker.DoWork += backgroundWorker_DoWork;
        backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;

        UpdateStatus(
            "Loading content....",
            0,
            GameEngineStatus.Starting);

        backgroundWorker.RunWorkerAsync();
    }

    public void Initialise()
    {
        _particleEngine.LoadContent();
    }

    private void backgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        if (sender is BackgroundWorker backgroundWorker)
        {
            backgroundWorker.DoWork -= backgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted -= backgroundWorker_RunWorkerCompleted;
            backgroundWorker.Dispose();
        }

        UpdateStatus(
            "Launching Game.....",
            100,
            GameEngineStatus.ContentLoaded);
    }

    private void backgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        UpdateStatus(
            "Loading content....",
            0,
            GameEngineStatus.Starting);

        var totalComponentsToLoad =
            _spriteDescriptions.Count +
            _dataDescriptions.Count +
            _animSpriteDescriptions.Count +
            _soundDescriptions.Count;

        var componentsLoaded = 0;

        LoadSpriteResources(totalComponentsToLoad, ref componentsLoaded);
        LoadAnimatedSpriteResources(totalComponentsToLoad, ref componentsLoaded);

        _particleEngine.LoadContent();

        LoadScreenData();
        LoadSoundResources(totalComponentsToLoad, ref componentsLoaded);

        UpdateStatus(
            "Launching Game.....",
            FullyLoadedPercent,
            GameEngineStatus.Starting);

        Thread.Sleep(LaunchPauseMs);
    }

    private void LoadSpriteResources(int totalComponentsToLoad, ref int componentsLoaded)
    {
        foreach (var t in _spriteDescriptions)
        {
            ReportProgress("sprite set", t.ResourceName, totalComponentsToLoad, componentsLoaded);

            _resources.Add(t.ResourceName, LoadSpriteCollection(t.FilePath, t.Width, t.Height));
            componentsLoaded++;
        }
    }

    private void LoadAnimatedSpriteResources(int totalComponentsToLoad, ref int componentsLoaded)
    {
        foreach (var t in _animSpriteDescriptions)
        {
            ReportProgress("anim sprite set", t.ResourceName, totalComponentsToLoad, componentsLoaded);

            _animationTileEngine.AnimTilesList.Add(
                new AnimTile(
                    _gameEngineInstance,
                    t.TileToAnimate,
                    LoadSpriteCollection(t.FilePath, t.Width, t.Height),
                    t.Delay));
            componentsLoaded++;
        }
    }

    private void LoadSoundResources(int totalComponentsToLoad, ref int componentsLoaded)
    {
        foreach (var t in _soundDescriptions)
        {
            ReportProgress("sound effect", t.ResourceName, totalComponentsToLoad, componentsLoaded);

            _soundEngine.LoadSoundEffect(t);
            componentsLoaded++;
        }
    }

    /// <summary>
    ///     The screens and the collision map they share, read straight out of one file with a plain
    ///     FileStream rather than through the content pipeline.
    /// </summary>
    /// <remarks>
    ///     Both arrays live in the same file, length-prefixed, and must be read in the order they were
    ///     written - which is why the two data resources are registered in order and read by index here.
    /// </remarks>
    private void LoadScreenData()
    {
        using var fs = new FileStream(_dataDescriptions[0].FilePath, FileMode.Open, FileAccess.Read);

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

        _resources.Add(
            _dataDescriptions[0].ResourceName,
            MessagePackSerializer.Deserialize<GameScreenData[]>(gameScreensData));
        _resources.Add(
            _dataDescriptions[1].ResourceName,
            MessagePackSerializer.Deserialize<int[,,]>(collisionTilesData));
    }

    /// <summary>
    ///     Builds one sprite collection. SpriteDescription and AnimSpriteDescription are unrelated types
    ///     that happen to carry the same three values, so this takes the values rather than either type.
    /// </summary>
    private SpriteCollection LoadSpriteCollection(string filePath, int width, int height)
        => new(
            _gameEngineInstance.GraphicsDeviceMgr,
            _contentManagerInstance,
            filePath,
            width,
            height);

    private void ReportProgress(string kindOfThing, string resourceName, int totalComponentsToLoad, int componentsLoaded)
        => UpdateStatus(
            $"Loading content, {kindOfThing}: '{resourceName}'",
            (int)(componentsLoaded / (double)totalComponentsToLoad * 100),
            GameEngineStatus.Starting);

    public void AddSpriteResource(string resourceName, string filePath, int spriteWidth, int spriteHeight)
    {
        _spriteDescriptions.Add(new SpriteDescription(resourceName, filePath, spriteWidth, spriteHeight));
    }

    public void AddAnimatedSpriteResource(string resourceName, string filePath, int spriteWidth, int spriteHeight, int tileToAnimate, int delay)
    {
        _animSpriteDescriptions.Add(new AnimSpriteDescription(resourceName, filePath, spriteWidth, spriteHeight, tileToAnimate, delay));
    }

    public void AddDataResource(string resourceName, string filePath, Type resourceType)
    {
        _dataDescriptions.Add(new DataDescription(resourceName, filePath, resourceType));
    }

    public void AddParticleResource(string effectName, string effectPath)
    {
        _particleEngine.AddEffect(effectName, effectPath);
    }

    public void AddSoundResource(string soundName, string soundPath, SoundEffectInstanceType instanceType, int maxInstances, bool killOnScreenChange)
    {
        _soundDescriptions.Add(new SoundDescription(soundName, soundPath, instanceType, maxInstances, killOnScreenChange));
    }

    public SpriteCollection GetSpriteCollection(string name)
    {
        if (!_resources.TryGetValue(name, out var resource))
        {
            throw new InvalidOperationException($"Sprite collection '{name}' has not been registered.");
        }

        return (SpriteCollection)resource;
    }

    public IParticleEffect GetParticleEffectInstance(string effectName, ParticleEffectType effectType, TimeSpan? duration)
    {
        if (effectType == ParticleEffectType.Continual)
        {
            return _particleEngine.CreateContinualEffectInstance(effectName, duration);
        }
        if (effectType == ParticleEffectType.Triggered)
        {
            return _particleEngine.CreateSingleTriggerEffectInstance(effectName);
        }
        if (effectType == ParticleEffectType.TriggeredMultiLocationTimed)
        {
            return _particleEngine.CreateMultiTriggeredTimeBasedEffectInstance(effectName);
        }

        throw new InvalidOperationException($"There is no particle effect of type '{effectType}'.");
    }

    public AnimTileEngine GetAnimationEngine()
        => _animationTileEngine;

    public SoundEffectEngine GetSoundFxEngine()
        => _soundEngine;

    public object GetDataResource(string name)
    {
        if (!_resources.TryGetValue(name, out var resource))
        {
            throw new InvalidOperationException($"Data resource '{name}' has not been registered.");
        }

        // Ensure we don't return a sprite array by mistake!
        if (resource is SpriteCollection)
        {
            throw new InvalidOperationException($"Resource '{name}' is a sprite collection, not a data resource.");
        }

        return resource;
    }
}