using UnityEngine;

public class WorkerNeeds : MonoBehaviour {
    [Range(0, 10)] [SerializeField] float thirst = 0f;
    [Range(0, 10)] [SerializeField] float bladder = 0f;

    [SerializeField] float thirstIntervalSeconds = 5f;
    [SerializeField] float bladderIntervalSeconds = 6f;

    public bool IsThirsty => thirst >= 6f;
    public bool NeedsBathroom => bladder >= 7f;

    CountdownTimer thirstTimer;
    CountdownTimer bladderTimer;

    void Start() {
        thirstTimer = new CountdownTimer(thirstIntervalSeconds);
        bladderTimer = new CountdownTimer(bladderIntervalSeconds);

        thirstTimer.OnTimerStop += () => { AdjustThirst(1f); thirstTimer.Start(); };
        bladderTimer.OnTimerStop += () => { AdjustBladder(1f); bladderTimer.Start(); };

        thirstTimer.Start();
        bladderTimer.Start();
    }

    void Update() {
        thirstTimer.Tick(Time.deltaTime);
        bladderTimer.Tick(Time.deltaTime);
    }

    public void Quench() => AdjustThirst(-10f);
    public void Relieve() => AdjustBladder(-10f);

    void AdjustThirst(float delta) => thirst = Mathf.Clamp(thirst + delta, 0f, 10f);
    void AdjustBladder(float delta) => bladder = Mathf.Clamp(bladder + delta, 0f, 10f);
}