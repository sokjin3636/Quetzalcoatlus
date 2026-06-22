using UnityEngine;

public class ZombieKnockdownState : IZombieState
{
    private float timer = 0f;
    private int currentPhase = 0;

    private float fallAndStayDuration = 3.0f;
    private float standUpDuration = 2.0f;

    public void Enter(ZombieController zombie)
    {
        timer = 0f;
        currentPhase = 0;
        zombie.Movement.Stop();

        zombie.Anim.CrossFade("Fall", 0.1f);

        if (zombie.voiceAudioSource != null && zombie.knockdownClip != null)
        {
            zombie.voiceAudioSource.PlayOneShot(zombie.knockdownClip);
        }

        if (zombie.moveAudioSource != null)
        {
            zombie.moveAudioSource.Stop();
        }
    }

    public void Execute(ZombieController zombie)
    {
        timer += Time.deltaTime;

        if (currentPhase == 0)
        {
            if (timer >= fallAndStayDuration)
            {
                zombie.Anim.CrossFade("StandUp", 0.1f);
                currentPhase = 1;
                timer = 0f;
            }
        }
        else if (currentPhase == 1)
        {
            if (timer >= standUpDuration)
            {
                zombie.ChangeState(new ZombiePatrolState(3.0f));
            }
        }
    }

    public void Exit(ZombieController zombie) { }
}