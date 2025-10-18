using System.Collections.Generic;
using UnityEngine;

public enum ControllerType
{
    Local,
    AI,
    Network,
    Replay,
    Dummy,
}

public sealed class InputSourceInstaller : MonoBehaviour
{
    public ControllerType controllerType = ControllerType.Local;

    [Header("Refs")]
    public InputBuffer inputBuffer;
    public CharacterProperty character;

    [Header("AI Settings")]
    public InputMacro_SO aiMacro;

    [Header("Replay Settings")]
    public ReplayRecorder replaySource; // (선택) 다른 객체에서 녹화된 걸 받아 재생하려면 null로 두고 나중에 세팅
    public bool replayLoop = true;

    // 네트워크 Provider는 외부 네트워크 레이어에서 가져와서 주입
    public NetworkInputProvider networkProviderExternal;

    void Awake()
    {
        if (inputBuffer == null) inputBuffer = GetComponent<InputBuffer>();
        if (character == null) character = GetComponent<CharacterProperty>();
    }

    private void Start()
    {
        ConfigureAndInstall(GameManager.Instance?.actions);
    }

    public void ConfigureAndInstall(InputSystem_Actions actions)
    {
        if (inputBuffer == null) inputBuffer = GetComponent<InputBuffer>();
        if (character == null) character = GetComponent<CharacterProperty>();

        var arbiter = new InputArbiter();

        switch (controllerType)
        {
            case ControllerType.Local:
                {
                    // 로컬 입력(장치) → LocalInputProvider
                    var local = new LocalInputProvider(actions, character);
                    arbiter.Register(local, priority: 100);
                    break;
                }
            case ControllerType.AI:
                {
                    var ai = new AIInputProvider(aiMacro, character);
                    arbiter.Register(ai, priority: 100);
                    break;
                }
            case ControllerType.Network:
                {
                    var net = networkProviderExternal ?? new NetworkInputProvider();
                    arbiter.Register(net, priority: 100);
                    networkProviderExternal = net; // 외부에서 접근 가능
                    break;
                }
            case ControllerType.Replay:
                {
                    //Dictionary<long, InputData> src = replaySource != null ? replaySource.GetDictionary() : null;
                    //var rep = new ReplayInputProvider(src, replayLoop);
                    //arbiter.Register(rep, priority: 100);
                    break;
                }

            case ControllerType.Dummy:
                {
                    inputBuffer.captureFromDevice = false;     // 확실히 디바이스 캡처 차단
                    inputBuffer.useArbiterSource = true;

                    var dummy = new DummyInputProvider();
                    arbiter.Register(dummy, priority: 100);         // 프로젝트 메서드명에 맞춰 호출
                    break;
                }
        }

        // InputBuffer를 Arbiter 소스로 전환
        inputBuffer.useArbiterSource = true;
        inputBuffer.arbiter = arbiter;
        inputBuffer.captureFromDevice = false; // Arbiter를 쓰므로 비활성
    }
}