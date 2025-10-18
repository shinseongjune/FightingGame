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
    public ReplayRecorder replaySource; // (����) �ٸ� ��ü���� ��ȭ�� �� �޾� ����Ϸ��� null�� �ΰ� ���߿� ����
    public bool replayLoop = true;

    // ��Ʈ��ũ Provider�� �ܺ� ��Ʈ��ũ ���̾�� �����ͼ� ����
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
                    // ���� �Է�(��ġ) �� LocalInputProvider
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
                    networkProviderExternal = net; // �ܺο��� ���� ����
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
                    inputBuffer.captureFromDevice = false;     // Ȯ���� ����̽� ĸó ����
                    inputBuffer.useArbiterSource = true;

                    var dummy = new DummyInputProvider();
                    arbiter.Register(dummy, priority: 100);         // ������Ʈ �޼���� ���� ȣ��
                    break;
                }
        }

        // InputBuffer�� Arbiter �ҽ��� ��ȯ
        inputBuffer.useArbiterSource = true;
        inputBuffer.arbiter = arbiter;
        inputBuffer.captureFromDevice = false; // Arbiter�� ���Ƿ� ��Ȱ��
    }
}