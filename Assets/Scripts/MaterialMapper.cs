using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class MaterialMapper : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Material material;
    public Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        videoPlayer.started += _ =>
        {
            material.mainTexture = videoPlayer.texture;
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (videoPlayer.isPrepared)
        {
            var normalizedTime = (float)videoPlayer.frame / videoPlayer.frameCount;
            animator.Play("CubeAMoveCircle", 0, normalizedTime);
            // animator.GetCurrentAnimatorStateInfo()
            //var controller = animator.runtimeAnimatorController;


            //var clip = animator.GetCurrentAnimatorClipInfo(0).

            //var @event = clip.events[0];

            

            //animationState.time = (float)videoPlayer.frame / videoPlayer.frameCount;

            // animation.

            // animator.runtimeAnimatorController.animationClips[0].
            //animationState.time = (float)videoPlayer.frame / videoPlayer.frameCount;
            // material.mainTexture = videoPlayer.texture;
        }
    }
}
