using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VolumetricFogAndMist
{
    public class Item_Fog : MonoBehaviour
    {
        #region Variables

        private VolumetricFog VolumetricFog;
        private PlayerMovement PlayerMovement;

        public bool IsFog = false;

        #endregion

        #region Initialization

        private void Start()
        {
            VolumetricFog = GetComponent<VolumetricFog>();
            PlayerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            Fog();
            CoverRange();
        }

        #endregion

        #region Functions

        private void Fog()
        {
            VolumetricFog.density -= Time.deltaTime * 0.1f;
            IsFog = true;

            if (VolumetricFog.density <= 0)
            {
                IsFog = false;
                Destroy(this.gameObject, 1f);
            }
        }

        private void CoverRange()
        {
            Collider[] colls = Physics.OverlapSphere(transform.position, 10f, 1 << LayerMask.NameToLayer("Enemy"));

            foreach (var coll in colls)
            {
                if (IsFog == true && coll.GetComponent<EnemyAI_Mongol>().IsCover == false)
                {
                    coll.GetComponent<EnemyAI_Mongol>().Cover();
                }
                else if (IsFog == false)
                {
                    coll.GetComponent<EnemyAI_Mongol>().End_Cover();
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                PlayerMovement.IsStealth = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                PlayerMovement.IsStealth = false;
            }
        }

        private void OnDestroy()
        {
            PlayerMovement.IsStealth = false;
        }

        #endregion
    }
}
