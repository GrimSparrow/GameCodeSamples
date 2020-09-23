using ProjectX;
using UnityEngine;

public class ExaminableBase : InteractableBase, IExaminable, IInteractable
{
    [SerializeField] 
	private string objectName;

    public string ObjectName
    {
        get => objectName;
    }

    public override void OnInteract()
    {
        ExamineController.Instance.Examine(Prepare());
    }

    public virtual ExaminableBase Prepare()
    {
        var examineObject =  Instantiate(gameObject, Vector3.zero, transform.rotation);
        var collider = examineObject.GetComponent<Collider>();
        var rigid = examineObject.GetComponent<Rigidbody>();
        if (collider != null )
        {
            collider.enabled = false;
        }
        if (rigid != null )
        {
            Destroy(rigid);
        }

        return examineObject.GetComponent<ExaminableBase>();
    }

    public virtual void Drag(float speed)
    {
        float rotX = Input.GetAxis("Mouse X") * speed * Mathf.Deg2Rad;
        float rotY = Input.GetAxis("Mouse Y") * speed * Mathf.Deg2Rad;
        
        transform.RotateAround(Vector3.up, -rotX);
        transform.RotateAround(Vector3.right, rotY);
    }

    public virtual void Use()
    {
        //Do Nothing
    }
}
