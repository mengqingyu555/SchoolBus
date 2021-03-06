using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace SchoolBusWXWeb.Models.SchollBusModels
{
    [Table("public.tterm")]
    public class tterm
    {
       
        [Key]
        [StringLength(36)]
        public string pkid
        {
            get => string.IsNullOrEmpty(_pkid) ? Guid.NewGuid().ToString("N") : _pkid.TrimEnd();
            set => _pkid = !string.IsNullOrEmpty(value) ? value : Guid.NewGuid().ToString("N");
        }
        private string _pkid;

        [Required]
        [StringLength(20)]
        public string fcode { get; set; }

        [Required]
        [StringLength(50)]
        public string fname { get; set; }

        public DateTime fstartdate { get; set; }

        public DateTime fenddate { get; set; }

        [StringLength(200)]
        public string fremark { get; set; }

        public DateTime fcreatetime { get; set; }

        #region �������ֶ�

        /// <summary>
        /// ttermpay.fcode
        /// </summary>
        [StringLength(50)]
        public string cardNum { get;set;}
        #endregion
    }
}
