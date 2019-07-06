using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace SchoolBusWXWeb.Models.SchollBusModels
{
    [Table("public.tcardlog")]
    public class tcardlog
    {
       
        [Key]
        [StringLength(36)]
        public string pkid
        {
            get => string.IsNullOrEmpty(_pkid) ? Guid.NewGuid().ToString("N") : _pkid.TrimEnd();
            set => _pkid = !string.IsNullOrEmpty(value) ? value : Guid.NewGuid().ToString("N");
        }
        private string _pkid;

        /// <summary>
        /// public.tdevice ��� fcode �豸����
        /// </summary>
        [Required]
        [StringLength(20)]
        public string fcode { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        [StringLength(50)]
        public string fid { get; set; }

        /// <summary>
        ///����ʱ��
        /// </summary>
        public DateTime fcreatetime { get; set; }

        /// <summary>
        /// ����
        /// </summary>
        public decimal? flng { get; set; }

        /// <summary>
        /// γ��
        /// </summary>
        public decimal? flat { get; set; }

        public int ftype { get; set; }
    }
}
