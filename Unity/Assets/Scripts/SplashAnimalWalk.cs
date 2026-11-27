
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class SplashAnimalWalk : MonoBehaviour
{
    private const float ShakeTime = .8f;
    private PlayableGraph graph;
    private AnimationClipPlayable walk;
    private Transform animal, entryBush, exitBush;
    private Vector3 start, end, modelPosition;
    private Quaternion modelRotation, entryRotation, exitRotation;
    private float startedAt, travelTime, waitTime;

    public void Configure(Transform model, AnimationClip clip, Vector3 destination,
        Transform entrance, Transform exit, float initialDelay)
    {
        animal = model;
        start = transform.position;
        end = destination;
        transform.rotation = Quaternion.LookRotation(end - start);
        modelPosition = model.localPosition;
        modelRotation = model.localRotation;
        entryBush = entrance;
        exitBush = exit;
        if (entryBush != null) entryRotation = entryBush.localRotation;
        if (exitBush != null) exitRotation = exitBush.localRotation;
        travelTime = Vector3.Distance(start, end) / 1.42f;
        waitTime = 3f;
        startedAt = Time.time + initialDelay;

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.gameObject.AddComponent<Animator>();
        animator.applyRootMotion = false;
        graph = PlayableGraph.Create(model.name + " / caminar");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        walk = AnimationClipPlayable.Create(graph, clip);
        AnimationPlayableOutput.Create(graph, "Caminar", animator).SetSourcePlayable(walk);
        graph.Play();
        animal.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!graph.IsValid() || Time.time < startedAt) return;
        float time = Mathf.Repeat(Time.time - startedAt, travelTime + waitTime);
        Shake(entryBush, entryRotation, time);
        Shake(exitBush, exitRotation, time - travelTime + ShakeTime * .6f);

        bool crossing = time < travelTime;
        animal.gameObject.SetActive(crossing);
        if (!crossing) return;

        transform.position = Vector3.Lerp(start, end, time / travelTime);
        walk.SetTime(Mathf.Repeat(time, walk.GetAnimationClip().length));
        graph.Evaluate(0f);
        animal.SetLocalPositionAndRotation(modelPosition, modelRotation);
    }

    private static void Shake(Transform bush, Quaternion restingRotation, float time)
    {
        if (bush == null) return;
        float progress = Mathf.Clamp01(time / ShakeTime);
        float amplitude = Mathf.Sin(progress * Mathf.PI);
        float angle = Mathf.Sin(progress * Mathf.PI * 6f) * amplitude * 9f;
        bush.localRotation = restingRotation * Quaternion.Euler(angle * .65f, 0f, angle);
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}

