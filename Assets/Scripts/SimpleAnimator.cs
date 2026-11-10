using UnityEngine;

/// <summary>Plays a looping or one-shot list of sprites on a SpriteRenderer (no Animator needed).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SimpleAnimator : MonoBehaviour
{
    SpriteRenderer sr;
    Sprite[] frames;
    float fps = 8f;
    bool loop = true;
    float time;
    int lastIndex = -1;

    public bool Finished { get; private set; }
    public Sprite[] Current => frames;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    public void Play(Sprite[] newFrames, float framesPerSecond, bool looping = true, bool restart = false)
    {
        if (!restart && newFrames == frames) { fps = framesPerSecond; return; }
        frames = newFrames;
        fps = framesPerSecond;
        loop = looping;
        time = 0f;
        lastIndex = -1;
        Finished = false;
        Apply();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        time += Time.deltaTime;
        Apply();
    }

    void Apply()
    {
        if (frames == null || frames.Length == 0) return;
        int index = Mathf.FloorToInt(time * fps);
        if (loop) index %= frames.Length;
        else if (index >= frames.Length) { index = frames.Length - 1; Finished = true; }

        if (index != lastIndex)
        {
            sr.sprite = frames[index];
            lastIndex = index;
        }
    }
}
