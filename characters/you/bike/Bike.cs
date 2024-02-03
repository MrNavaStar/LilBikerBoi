using Godot;
using LilBikerBoi.characters.you.player;

namespace LilBikerBoi.characters.you.bike;

public partial class Bike : CharacterBody3D, IInteractible
{
	public bool MovementActive;

	[Export]
	private float _turnSpeed = 0.7f;	
	[Export]
	private float _maxTurnDelta = Mathf.Pi/6;	
	[Export]
	private float _moveSpeed = 25f;
	[Export]
	private float _accelerationRate = 0.1f;
	[Export]
	private float _frictionRate = 1.2f;
	[Export]
	private float _brakeFrictionRate = 6f;

	private Sprite3D _sprite;
	private CameraMovement _camera;
	private Player _player;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite3D>("Sprite3D");
		_camera = GetNode<CameraMovement>("/root/Game/World/PlayerPosition/Camera3D");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = MovementActive ? Input.GetVector("left", "right", "down", "up") : Vector2.Zero;
		RotateBike(direction, (float)delta);
		MoveBikeForward(direction, (float)delta);
	}

	public void InputFromParent(InputEvent @event)
	{
		if (!MovementActive) {
			return;
		}

		if (@event.IsActionPressed("interact") || @event.IsActionPressed("back"))
		{
			MovementActive = _player.ridingBike = false;
			_camera.EmitSignal("ChangeCameraToPlayer");
			_player.FinishInteraction();
		}
	}

	private void RotateBike(Vector2 direction, float delta) {
		// TODO: add some sort of 'memory', so if you turn 180 it remembers 
		//   whether your last rotation was clockwise or counter?
		if (direction != Vector2.Zero) {
			float currentSpeed = Mathf.Sqrt(Mathf.Pow(Velocity.X, 2) + Mathf.Pow(Velocity.Z, 2));

			float clampedDifference = Mathf.Clamp(Mathf.AngleDifference(Rotation.Y, direction.Angle()), -_maxTurnDelta, _maxTurnDelta);
			float interpolatedAngle = Mathf.LerpAngle(Rotation.Y, Rotation.Y + clampedDifference, _turnSpeed * delta * currentSpeed);
			Vector3 newRotation = new Vector3(0, interpolatedAngle, 0);
			Rotation = newRotation;
		}
	}

	private void MoveBikeForward(Vector2 direction, float delta) {
		// grab the speed/radius of the current and input velocities
		// interpolate the speed between the current and input speeds
		// apply it to the forward direction/angle of the bike's rotation

		float currentSpeed = Mathf.Sqrt(Mathf.Pow(Velocity.X, 2) + Mathf.Pow(Velocity.Z, 2));
		float inputSpeed = Mathf.Sqrt(Mathf.Pow(direction.X, 2) + Mathf.Pow(direction.Y, 2)) * _moveSpeed;

		float speed = currentSpeed;
		if (inputSpeed > currentSpeed)
			speed = Mathf.Lerp(currentSpeed, inputSpeed, _accelerationRate * delta * _moveSpeed);

		float brakeRatio = (float) Mathf.Clamp(Input.GetActionStrength("brake"), 0.5, 1);
		if (brakeRatio > 0.5){
			Velocity = Velocity.Lerp(Vector3.Zero, brakeRatio * _brakeFrictionRate * delta);
		} else {
			Velocity = new Vector3(Mathf.Cos(Rotation.Y), 0, -Mathf.Sin(Rotation.Y)) * speed;
		}

		Velocity = Velocity.Lerp(Vector3.Zero, _frictionRate * delta);

		MoveAndSlide();
	}

	void IInteractible.Interact(Player player) {
		_player = player;
		MovementActive = _player.ridingBike = true;
		_camera.EmitSignal("ChangeCameraToBike");
	}
}
