using Fusion;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
{
  [SerializeField] private Ball _prefabBall;
  [SerializeField] private PhysxBall _prefabPhysxBall;

  [Networked] private TickTimer delay { get; set; }
  [Networked] public bool spawnedProjectile { get; set; }
  private NetworkCharacterController _cc;
  private Vector3 _forward;
  private ChangeDetector _changeDetector;
  public Material _material;

  private TMP_Text _messages;

  [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
  public void RPC_SendMessage(string message, RpcInfo info = default)
  {
    RPC_RelayMessage(message, info.Source);
  }

  [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
  public void RPC_RelayMessage(string message, PlayerRef messageSource)
  {
    if (_messages == null)
      _messages = FindObjectOfType<TMP_Text>();

    if (messageSource == Runner.LocalPlayer)
    {
      message = $"You said: {message}\n";
    }
    else
    {
      message = $"Some other player said: {message}\n";
    }

    _messages.text += message;
  }
  private void Update()
  {
    if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.R))
    {
      RPC_SendMessage("Heyyyyyyyyyy!");
    }
  }
  public override void Spawned()
  {
    _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    Runner.Spawn(_prefabBall, transform.position + _forward, Quaternion.LookRotation(_forward));
    spawnedProjectile = !spawnedProjectile;
  }
  private void Awake()
  {
    _cc = GetComponent<NetworkCharacterController>();
    _forward = transform.forward;
    _material = GetComponentInChildren<MeshRenderer>().material;
  }

  public override void Render()
  {
    foreach (var change in _changeDetector.DetectChanges(this))
    {
      switch (change)
      {
        case nameof(spawnedProjectile):
          _material.color = Color.white;
          break;
      }
    }
    _material.color = Color.Lerp(_material.color, Color.blue, Time.deltaTime);
  }
  public override void FixedUpdateNetwork()
  {
    if (GetInput(out NetworkInputData data))
    {
      data.direction.Normalize();
      _cc.Move(5 * data.direction * Runner.DeltaTime);

      if (data.direction.sqrMagnitude > 0)
        _forward = data.direction;

      if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
      {
        if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
        {
          delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
          Runner.Spawn(_prefabBall,
            transform.position + _forward,
            Quaternion.LookRotation(_forward),
            Object.InputAuthority,
            (runner, o) =>
            {
              // Initialize the Ball before synchronizing it
              o.GetComponent<Ball>().Init();
            });
        }
        else if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON1))
        {
          delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
          Runner.Spawn(_prefabPhysxBall,
            transform.position + _forward,
            Quaternion.LookRotation(_forward),
            Object.InputAuthority,
            (runner, o) =>
            {
              o.GetComponent<PhysxBall>().Init(10 * _forward);
            });
        }
      }
    }
  }
}