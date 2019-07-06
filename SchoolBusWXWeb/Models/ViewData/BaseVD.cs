﻿using System.Collections.Generic;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SchoolBusWXWeb.Models.ViewData
{
    public class BaseVD
    {
        public int status { get; set; }
        public string msg { get; set; }
    }

    public class RegisVD : BaseVD { }

    public class SaveCardInfoVD : BaseVD { }

    public class SmsVD : BaseVD { }

    public class SchoolVD : BaseVD
    {
        public List<SchoolMode> data { get; set; }
    }

    public class SchoolBaseInfo
    {
        public int ftype { get; set; }
        public string text { get; set; }
        public string value { get; set; }

    }

    public class SchoolMode
    {
        public string value { get; set; }
        public string text { get; set; }
        public List<SchoolValueText> children { get; set; }
    }

    public class SchoolValueText
    {
        private string _value;
        public string value
        {
            get => string.IsNullOrEmpty(_value) ? "" : _value.TrimEnd();
            set => _value = !string.IsNullOrEmpty(value) ? value : "";
        }
        public string text { get; set; }
    }

    public class AddressModel
    {
        /// <summary>
        /// 用户状态标识
        /// </summary>
        public int status { get; set; } = 99;

        /// <summary>
        /// 学生姓名
        /// </summary>
        public string student { get;set;}

        /// <summary>
        /// 0:刷卡位置 1:实时位置
        /// </summary>
        public int showType { get; set; }

        /// <summary>
        /// 刷卡位置:public.tcardlog 表 主键
        /// </summary>
        public string cardLogId { get;set;}

        /// <summary>
        /// public.tdevice 表的 设备编码
        /// </summary>
        public string fcode { get;set;}

        /// <summary>
        /// 纬度
        /// </summary>
        public decimal? flat { get;set;}

        /// <summary>
        /// 经度
        /// </summary>
        public decimal? flng { get; set; }
        
        /// <summary>
        /// 微信分享链接
        /// </summary>
        public string wxLink { get; set; }

        /// <summary>
        /// 微信分享图标
        /// </summary>
        public string wximgUrl { get; set; }

        /// <summary>
        /// 微信分享标题
        /// </summary>
        public string wxshareTitle { get; set; }

        /// <summary>
        /// 微信分享描述
        /// </summary>
        public string wxshareDescription { get; set; }
    }
}
