using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanTopServer.Models
{
    internal class SuffrageVerify
    {
        /// <summary>
        /// 通过列表
        /// </summary>
        private List<string> passed = new List<string>();

        /// <summary>
        /// 通过数
        /// </summary>
        public int PassedCount { get { return passed.Count; } }

        /// <summary>
        /// 拒绝列表
        /// </summary>
        private List<string> denied = new List<string>();

        /// <summary>
        /// 拒绝数
        /// </summary>
        public int DeniedCount { get { return denied.Count; } }

        /// <summary>
        /// 添加通过列表，自动从拒绝列表删除
        /// </summary>
        /// <param name="id"></param>
        public bool AddPassed(string id)
        {
            if (denied.Contains(id))//如果存在于拒绝列表中
            {
                denied.Remove(id);
            }

            if (!passed.Contains(id))
            {
                passed.Add(id);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 添加拒绝列表，自动从允许列表中删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool AddDenied(string id)
        {
            if (passed.Contains(id))//如果存在于通过列表中
            {
                passed.Remove(id);
            }

            if (!denied.Contains(id))
            {
                denied.Add(id);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
