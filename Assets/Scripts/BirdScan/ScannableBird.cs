/*
    ScannableBird.cs: drives one Living Birds model during the bird sampling step.
    - Replaces lb_Bird (which needs lb_BirdController and room-scale physics) with a small loop sized for AR:
      fly to a landing spot on a real surface, idle there (sing, peck, preen, ruffle, hop), then fly to another.
      With no landing spot available it keeps flying between points in the air until one is found.
    - Holds the bird's genotype, which stays hidden from the student until they sample it.
    - Receives the PlaySong / ResetHopInt animation events baked into the Living Birds clips.
*/

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ScannableBird : MonoBehaviour
{
    public string genotype;

    [Header("Idle")]
    [Tooltip("Seconds spent idling on a spot before flying to another one.")]
    public Vector2 idleSecondsRange = new Vector2(6f, 14f);
    [Tooltip("Average number of idle behaviors (sing, peck, hop...) started per second.")]
    public float behaviorsPerSecond = 0.33f;
    [Tooltip("If root-motion hops carry the bird this far (m) from where it landed, it flies somewhere new.")]
    public float maxHopDrift = 0.3f;

    [Header("Flight")]
    public float flySpeed = 1.2f;
    [Tooltip("How high (m) above the straight line between points the flight arc peaks.")]
    public Vector2 arcHeightRange = new Vector2(0.25f, 0.6f);
    [Tooltip("Seconds before touchdown that the landing animation starts.")]
    public float landingSeconds = 0.45f;
    public float turnSharpness = 8f;

    [Header("Audio")]
    public AudioClip song1;
    public AudioClip song2;
    public AudioClip flyAway1;
    public AudioClip flyAway2;

    public bool Sampled { get; private set; }
    public bool IsFlying { get; private set; }

    // Visible mesh bounds, used for on-screen detection
    public Bounds ScanBounds => meshRenderer != null ? meshRenderer.bounds : new Bounds(transform.position, Vector3.one * 0.1f);

    private BirdScanManager manager;
    private Animator anim;
    private AudioSource audioSource;
    private Renderer meshRenderer;
    private Vector3 landedAt;

    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.Idle");
    private static readonly int FlyStateHash = Animator.StringToHash("Base Layer.fly");
    private static readonly int FlyingHash = Animator.StringToHash("flying");
    private static readonly int LandingHash = Animator.StringToHash("landing");
    private static readonly int HopHash = Animator.StringToHash("hop");
    private static readonly int IdleAgitatedHash = Animator.StringToHash("IdleAgitated");
    private static readonly int PeckHash = Animator.StringToHash("peck");
    private static readonly int RuffleHash = Animator.StringToHash("ruffle");
    private static readonly int PreenHash = Animator.StringToHash("preen");
    private static readonly int SingHash = Animator.StringToHash("sing");
    private static readonly int FlyingDirectionXHash = Animator.StringToHash("flyingDirectionX");

    // Birds start in the air at startPosition and fly in to land
    public void Init(BirdScanManager owner, string birdGenotype, Vector3 startPosition)
    {
        manager = owner;
        genotype = birdGenotype;

        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<Renderer>();

        transform.position = startPosition;
        transform.rotation = FacingViewer();
        anim.applyRootMotion = false;
        anim.SetFloat(IdleAgitatedHash, Random.value);
        anim.SetBool(FlyingHash, true);
        IsFlying = true;

        StartCoroutine(Life());
    }

    public void MarkSampled()
    {
        Sampled = true;
        if (!IsFlying)
            anim.SetTrigger(SingHash);
    }

    private IEnumerator Life()
    {
        while (true)
        {
            if (manager.TryClaimLandingSpot(this, out Vector3 spot))
            {
                yield return FlyTo(spot, true);

                float idleUntil = Time.time + Random.Range(idleSecondsRange.x, idleSecondsRange.y);
                while (Time.time < idleUntil && !DriftedFromLanding())
                {
                    IdleTick();
                    yield return null;
                }
            }
            else
            {
                yield return FlyTo(manager.AirWaypoint(), false);
            }
        }
    }

    private bool DriftedFromLanding()
    {
        return (transform.position - landedAt).sqrMagnitude > maxHopDrift * maxHopDrift;
    }

    private void IdleTick()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).fullPathHash != IdleStateHash)
            return;
        if (Random.value >= Time.deltaTime * behaviorsPerSecond)
            return;

        float rand = Random.value;
        if (rand < .3f) anim.SetTrigger(SingHash);
        else if (rand < .5f) anim.SetTrigger(PeckHash);
        else if (rand < .6f) anim.SetTrigger(PreenHash);
        else if (rand < .7f) anim.SetTrigger(RuffleHash);
        else if (rand < .85f) anim.SetInteger(HopHash, 1);   // forward
        else if (rand < .9f) anim.SetInteger(HopHash, -2);   // left
        else if (rand < .95f) anim.SetInteger(HopHash, 2);   // right
        else anim.SetInteger(HopHash, -1);                   // back

        // Mix the idle blend tree differently next time
        anim.SetFloat(IdleAgitatedHash, Random.value);
    }

    // Fly along an arc to target. land = touch down and switch back to idle; otherwise keep flying.
    private IEnumerator FlyTo(Vector3 target, bool land)
    {
        if (!IsFlying)
        {
            IsFlying = true;
            PlayOneShot(Random.value < .5f ? flyAway1 : flyAway2, 0.3f);

            anim.applyRootMotion = false;
            anim.SetInteger(HopHash, 0);
            anim.SetBool(LandingHash, false);
            anim.SetBool(FlyingHash, true);

            // Let the takeoff transition start flapping before the bird moves
            float waited = 0f;
            while (anim.GetCurrentAnimatorStateInfo(0).fullPathHash != FlyStateHash && waited < 0.5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        Vector3 start = transform.position;
        Vector3 control = (start + target) * 0.5f + Vector3.up * Random.Range(arcHeightRange.x, arcHeightRange.y);
        float arcLength = Vector3.Distance(start, control) + Vector3.Distance(control, target);
        float duration = Mathf.Max(0.8f, arcLength / flySpeed);

        bool landingStarted = false;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            // Ease in and out a little when taking off and landing
            float linear = elapsed / duration;
            float t = land ? Mathf.Lerp(linear, Mathf.SmoothStep(0f, 1f, linear), 0.5f) : linear;
            transform.position = Bezier(start, control, target, t);

            Vector3 heading = BezierTangent(start, control, target, t);
            if (heading.sqrMagnitude > 1e-6f)
            {
                Quaternion look = Quaternion.LookRotation(heading.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-turnSharpness * Time.deltaTime));
            }
            anim.SetFloat(FlyingDirectionXHash, BankingAngle(transform.forward, (target - transform.position).normalized));

            if (land && !landingStarted && duration - elapsed <= landingSeconds)
            {
                landingStarted = true;
                anim.SetFloat(FlyingDirectionXHash, 0f);
                anim.SetBool(FlyingHash, false);
                anim.SetBool(LandingHash, true);
            }
            yield return null;
        }

        transform.position = target;
        if (!land)
            yield break;

        anim.SetFloat(FlyingDirectionXHash, 0f);

        // Level out and turn to face roughly toward the student
        Quaternion from = transform.rotation;
        Quaternion to = FacingViewer();
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
        {
            transform.rotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
        transform.rotation = to;

        anim.SetBool(LandingHash, false);
        anim.applyRootMotion = true;
        landedAt = target;
        IsFlying = false;
    }

    // Yaw toward the viewer with some randomness so students see the bird's side profile
    private Quaternion FacingViewer()
    {
        Vector3 toViewer = manager.ViewerPosition - transform.position;
        toViewer.y = 0f;
        if (toViewer.sqrMagnitude < 1e-6f)
            toViewer = Vector3.forward;
        return Quaternion.LookRotation(toViewer.normalized, Vector3.up) * Quaternion.Euler(0f, Random.Range(-70f, 70f), 0f);
    }

    // -1..1 for the left/right banking blend while flying
    private static float BankingAngle(Vector3 birdForward, Vector3 dirToTarget)
    {
        return Vector3.Dot(Vector3.Cross(birdForward, dirToTarget), Vector3.up);
    }

    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private static Vector3 BezierTangent(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return 2f * (1f - t) * (b - a) + 2f * t * (c - b);
    }

    private void PlayOneShot(AudioClip clip, float volume)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }

    // Animation event (sing clip)
    private void PlaySong()
    {
        PlayOneShot(Random.value < .5f ? song1 : song2, 1f);
    }

    // Animation event (hop clips)
    private void ResetHopInt()
    {
        anim.SetInteger(HopHash, 0);
    }
}
