// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Sounds;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ContentManagement;

internal class ResourceManager : StartableGameComponentBase
{
    /// <summary>
    ///     The percentage reported once everything has loaded, and how long the "Launching Game" message
    ///     is left up before the game takes over - long enough to be read rather than flash past.
    /// </summary>
    private const int FullyLoadedPercent = 100;

    private readonly AnimTileEngine _animationTileEngine;
    private readonly List<AnimSpriteDescription> _animSpriteDescriptions;
    private readonly ContentManager _contentManagerInstance;
    private readonly List<DataDescription> _dataDescriptions;
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly GraphicsSet _graphicsSet;
    private readonly ParticleEngine _particleEngine;

    private readonly Dictionary<string, object> _resources;
    private readonly List<SoundDescription> _soundDescriptions;

    // Sound Engine
    private readonly SoundEffectEngine _soundEngine;

    private readonly List<SpriteDescription> _spriteDescriptions;

    private volatile Exception? _loadFailure;

    public ResourceManager(
        ContentManager content,
        GraphicsDeviceManager graphics,
        GraphicsSet graphicsSet)
    {
        _contentManagerInstance = content;
        _graphicsDeviceManager = graphics;
        _graphicsSet = graphicsSet;

        _particleEngine = new ParticleEngine(content, graphics, graphicsSet);
        _animationTileEngine = new AnimTileEngine();
        // Random.Shared rather than the game's own generator: the delay between background
        // rumbles is drawn on a thread pool timer callback, and System.Random is not thread-safe -
        // concurrent use can corrupt its state so that it returns zero indefinitely, and about a
        // dozen collaborators draw from the game's instance every frame.
        _soundEngine = new SoundEffectEngine(content);

        _spriteDescriptions = new List<SpriteDescription>();
        _animSpriteDescriptions = new List<AnimSpriteDescription>();
        _dataDescriptions = new List<DataDescription>();
        _soundDescriptions = new List<SoundDescription>();

        _resources = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Starts loading everything that has been registered, on a background thread, and returns
    ///     at once. The caller keeps drawing the loading screen and watches <c>Status</c> for
    ///     <see cref="GameEngineStatus.ContentLoaded" />.
    ///     <para>
    ///         Loading off the main thread is safe here because MonoGame marshals the graphics work
    ///         back for us: <c>Texture2D</c> does its GL calls through
    ///         <c>Threading.BlockOnUIThread</c>, and the queue that feeds is pumped once per
    ///         iteration of the platform's run loop - which keeps turning, because the main thread
    ///         is busy animating the loading screen rather than waiting for this. The one graphics
    ///         call MonoGame does <em>not</em> marshal, constructing a <c>SpriteBatch</c>, is
    ///         deferred to the first draw - see <c>SpriteCollection.Sprites</c>.
    ///     </para>
    /// </summary>
    public void LoadContent()
    {
        _resources.Clear();

        UpdateStatus(
            "Loading content....",
            0,
            GameEngineStatus.Starting);

        _ = LoadContentInBackground();
    }

    private async Task LoadContentInBackground()
    {
        try
        {
            await Task.Run(LoadEverything).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // The BackgroundWorker this replaced put the exception in RunWorkerCompletedEventArgs
            // and nothing ever looked at it, so a failure to load left the game running on missing
            // content and failing somewhere else entirely. Rethrowing on the main thread means a
            // missing file says so, at the point it went wrong.
            _loadFailure = exception;

            return;
        }

        UpdateStatus(
            "Launching Game.....",
            FullyLoadedPercent,
            GameEngineStatus.ContentLoaded);
    }

    /// <summary>
    ///     Rethrows anything the background load threw, on whichever thread asks. Called from the
    ///     game's update so a load failure surfaces in the loop rather than vanishing.
    /// </summary>
    public void ThrowIfLoadFailed()
    {
        var failure = _loadFailure;

        if (failure != null)
        {
            _loadFailure = null;

            throw new InvalidOperationException("The game's content could not be loaded.", failure);
        }
    }

    public void Initialise()
    {
        _particleEngine.LoadContent();
    }

    private void LoadEverything()
    {
        int totalComponentsToLoad =
            _spriteDescriptions.Count +
            _dataDescriptions.Count +
            _animSpriteDescriptions.Count +
            _soundDescriptions.Count;

        int componentsLoaded = 0;

        LoadSpriteResources(totalComponentsToLoad, ref componentsLoaded);
        LoadAnimatedSpriteResources(totalComponentsToLoad, ref componentsLoaded);

        _particleEngine.LoadContent();

        LoadScreenData();
        LoadSoundResources(totalComponentsToLoad, ref componentsLoaded);

        UpdateStatus(
            "Launching Game.....",
            FullyLoadedPercent,
            GameEngineStatus.Starting);

    }

    private void LoadSpriteResources(int totalComponentsToLoad, ref int componentsLoaded)
    {
        foreach (SpriteDescription t in _spriteDescriptions)
        {
            ReportProgress("sprite set", t.ResourceName, totalComponentsToLoad, componentsLoaded);

            _resources.Add(t.ResourceName, LoadSpriteCollection(t.FilePath, t.Width, t.Height));
            componentsLoaded++;
        }
    }

    private void LoadAnimatedSpriteResources(int totalComponentsToLoad, ref int componentsLoaded)
    {
        foreach (AnimSpriteDescription t in _animSpriteDescriptions)
        {
            ReportProgress("anim sprite set", t.ResourceName, totalComponentsToLoad, componentsLoaded);

            _animationTileEngine.AnimTilesList.Add(
                new AnimTile(
                    _graphicsSet,
                    t.TileToAnimate,
                    LoadSpriteCollection(t.FilePath, t.Width, t.Height),
                    t.Delay));
            componentsLoaded++;
        }
    }

    private void LoadSoundResources(int totalComponentsToLoad, ref int componentsLoaded)
    {
        foreach (SoundDescription t in _soundDescriptions)
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
        byte[] lengthBuffer = new byte[sizeof(int)];
        fs.ReadExactly(lengthBuffer, 0, sizeof(int));
        int gameScreensArraySize = BitConverter.ToInt32(lengthBuffer, 0);

        // Read the first array
        byte[] gameScreensData = new byte[gameScreensArraySize];
        fs.ReadExactly(gameScreensData, 0, gameScreensArraySize);

        // Read the length of the second array
        fs.ReadExactly(lengthBuffer, 0, sizeof(int));
        int collisionTilesArraySize = BitConverter.ToInt32(lengthBuffer, 0);

        // Read the second array
        byte[] collisionTilesData = new byte[collisionTilesArraySize];
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
            _graphicsDeviceManager,
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

    public void AddDataResource(string resourceName, string filePath)
    {
        _dataDescriptions.Add(new DataDescription(resourceName, filePath));
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
        if (!_resources.TryGetValue(name, out object? resource))
        {
            throw new InvalidOperationException($"Sprite collection '{name}' has not been registered.");
        }

        return (SpriteCollection)resource;
    }

    /// <summary>
    ///     A pixel shader, loaded the first time it is asked for and kept thereafter.
    /// </summary>
    /// <remarks>
    ///     Loaded on demand rather than registered with everything else, for two reasons. Building
    ///     an <see cref="Effect" /> compiles and links on the GPU, and <c>LoadEverything</c> runs on
    ///     a background thread where only some graphics calls are marshalled back - the same
    ///     hazard <c>SpriteCollection.Sprites</c> defers around. And a shader is wanted by the
    ///     thing drawing with it rather than by the game as a whole, so the first draw is both a
    ///     safe moment and an honest one.
    /// </remarks>
    public Effect GetEffect(string path)
    {
        if (_resources.TryGetValue(path, out object? loaded))
        {
            return (Effect)loaded;
        }

        var effect = _contentManagerInstance.Load<Effect>(path);
        _resources[path] = effect;

        return effect;
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
        if (!_resources.TryGetValue(name, out object? resource))
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
