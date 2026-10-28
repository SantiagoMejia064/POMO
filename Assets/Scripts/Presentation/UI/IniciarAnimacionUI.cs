using UnityEngine;

public class IniciarAnimacionUI : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (animator != null)
        {
            animator.Play(0, 0, 0f);
        }
    }
}