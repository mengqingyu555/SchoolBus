﻿using SchoolBusWXWeb.Models;
using SchoolBusWXWeb.Models.PmsData;
using SchoolBusWXWeb.Models.SchollBusModels;
using SchoolBusWXWeb.Models.ViewData;
using SchoolBusWXWeb.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

// ReSharper disable SwitchStatementMissingSomeCases

namespace SchoolBusWXWeb.Business
{
    public class SchoolBusBusines : ISchoolBusBusines
    {
        private readonly SiteConfig _option;
        private readonly ISchoolBusRepository _schoolBusRepository;
        public SchoolBusBusines(ISchoolBusRepository schoolBusRepository, IOptions<SiteConfig> option)
        {
            _option = option.Value;
            _schoolBusRepository = schoolBusRepository;
        }

        /// <summary>
        /// TODO 根据主键获取用户信息
        /// </summary>
        /// <param name="pkid"></param>
        /// <returns></returns>
        public async Task<twxuser> GetTwxuserAsync(string pkid)
        {
            try
            {
                return await _schoolBusRepository.GetTwxuserBypkidAsync(pkid);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// TODO 用户注册
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<RegisVD> DoRegisterAsync(RegisterModel user)
        {
            #region 卡号校验
            var cardRecord = await _schoolBusRepository.GetCardByCodeAsync(user.cardNum);
            if (cardRecord == null)
            {
                return new RegisVD { msg = "卡号错误，请重新输入" };
            }
            switch (cardRecord.fstatus)
            {
                case 2:
                    return new RegisVD { msg = "该卡已挂失，请重新输入" };
                case 3:
                    return new RegisVD { msg = "该卡已注销，请重新输入" };
            }
            #endregion

            #region 验证码校验
            DateTime date = DateTime.Now;
            DateTime beforedate = date.AddMinutes(-10);
            var codeList = await _schoolBusRepository.GetSmsListBySendTimeAsync(user.phoneNum, 0, beforedate, date);
            if (codeList.Count > 0)
            {
                var codeM = codeList.FirstOrDefault(c => c.fvcode == user.verificationCode);
                if (codeM == null)
                {
                    return new RegisVD { msg = "验证码错误，请重新输入" };
                }
                if (date > codeM.finvalidtime)
                {
                    return new RegisVD { msg = "验证码超时" };
                }
            }
            else
            {
                return new RegisVD { msg = "验证码超时" };
            }
            #endregion

            #region 微信号校验和注册操作
            var userRecord = await _schoolBusRepository.GetTwxuserBytOpenidAsync(user.wxid);
            if (userRecord == null) // 用户没有注册过
            {
                twxuser wxuser = new twxuser
                {
                    fwxid = user.wxid,
                    fname = user.userName,
                    fk_card_id = cardRecord.pkid,
                    frelationship = user.relationship,
                    fphone = user.phoneNum,
                    fstatus = 0
                };
                var res = await _schoolBusRepository.InsertWxUserAsync(wxuser);
                if (res == 0)
                {
                    return new RegisVD { msg = "注册失败,请稍后尝试" };
                }
            }
            else
            {
                // 根据当前用户微信获取之前绑卡卡号信息
                var userCardRecord = await _schoolBusRepository.GetCardBypkidAsync(userRecord.fk_card_id);// 这一步一定有卡信息
                if (userCardRecord == null)
                {
                    return new RegisVD { msg = "你注册的卡已经不存在,请联系管理员" };
                }
                if (userCardRecord.fstatus == 1)
                {
                    return new RegisVD { msg = "该微信已注册" };
                }

                // 老卡数据要导入新卡中
                if (!string.IsNullOrEmpty(cardRecord.fname) && !string.IsNullOrEmpty(userCardRecord.fname))
                {
                    cardRecord.fname = userCardRecord.fname;
                    cardRecord.fsex = userCardRecord.fsex;
                    cardRecord.fk_school_id = userCardRecord.fk_school_id;
                    cardRecord.fk_device_id = userCardRecord.fk_device_id;
                    cardRecord.fboardingaddress = userCardRecord.fboardingaddress;
                    cardRecord.fbirthdate = userCardRecord.fbirthdate;
                }
                // userCardRecord.pkid 之前卡片信息
                // cardRecord.pkid  新卡信息 
                // 更新所有绑定老卡用户卡片信息
                var s1 = await _schoolBusRepository.UpdateUserCardAsync(userCardRecord.pkid, cardRecord.pkid);
                if (s1 == 0)
                {
                    return new RegisVD { msg = "更新所有绑定老卡用户卡片信息" };
                }
                #region 更新已有用户信息
                userRecord.pkid = userRecord.pkid;
                userRecord.fk_card_id = cardRecord.pkid;
                userRecord.frelationship = user.relationship;
                userRecord.fphone = user.phoneNum;
                var s2 = await _schoolBusRepository.UpdateWxUserAsync(userRecord);
                if (s2 == 0)
                {
                    return new RegisVD { msg = "更新已有用户信息失败" };
                }
                #endregion
            }
            #endregion

            #region 卡片信息维护
            if (cardRecord.ftrialdate == null)
            {
                DateTime triald = Convert.ToDateTime(cardRecord.ftrialdate);
                var trialdateRecord = await _schoolBusRepository.GetSchoolConfigAsync("001"); // 首次注册试用期（天）
                int.TryParse(trialdateRecord.fvalue, out int tt);
                cardRecord.ftrialdate = triald.AddYears(tt);  // 卡片试用期赋值
            }
            cardRecord.fstatus = 1;   // 维护卡片信息状态 
            var s3 = await _schoolBusRepository.UpdateTCardAsync(cardRecord);
            #endregion
            return s3 == 0 ? new RegisVD { msg = "维护卡片信息失败" } : new RegisVD { status = 1, msg = "注册成功" };
        }

        /// <summary>
        /// TODO 发送短信验证码
        /// </summary>
        /// <param name="sms"></param>
        /// <returns></returns>
        public async Task<SmsVD> SendSmsCodeAsync(SmsModel sms)
        {
            DateTime date = DateTime.Now;
            DateTime beforedate = date.AddMinutes(-10);
            DateTime before1Mdate = date.AddMinutes(-1);
            DateTime invaliddate = date.AddMinutes(10); // 失效时间为10分钟
            // 获取十分钟内已发送验证码列表
            var codeList = await _schoolBusRepository.GetSmsListBySendTimeAsync(sms.phoneNum, sms.verificationCodeType, beforedate, date);
            // 验证发送次数
            if (codeList.Count >= 5)
            {
                return new SmsVD { msg = "短时间发送短信过多" };
            }
            // 发送间隔1分钟判断
            if (codeList.Any() && codeList.First().fsendtime > before1Mdate)
            {
                return new SmsVD { msg = "验证码已失效" };
            }

            // 生成6位随机数验证码
            Random ran = new Random();
            string code = ran.Next(100000, 999999).ToString();
#if DEBUG
            var smsresult = new AliSmsModel { Code = "OK" };
#else
            var smsresult = Tools.SendSms(sms.phoneNum, code);
#endif

            switch (smsresult.Code)
            {
                case "scfaile":
                    return new SmsVD { msg = smsresult.Message };
                case "OK":
                    {
                        tsms tsms = new tsms
                        {
                            fphone = sms.phoneNum,
                            fvcode = code,
                            fsendtime = date,
                            finvalidtime = invaliddate,
                            ftype = sms.verificationCodeType
                        };
                        await _schoolBusRepository.InsertSMSCodeAsync(tsms);
                        return new SmsVD { status = 1, msg = "发送成功" };
                    }
                default:
                    return new SmsVD { msg = "发送失败" };
            }

        }

        /// <summary>
        /// TODO 完善信息
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public async Task<UserAndCardModel> GetCardInfoByCodeAsync(string wxid)
        {
            var data = await _schoolBusRepository.GetUserAndCardByOpenidAsync(wxid);
            if (data == null) return new UserAndCardModel();
            var configList = await _schoolBusRepository.GetSchoolConfigListAsync("'002','003'");
            data.wxshareTitle = configList.FirstOrDefault(x => x.fcode == "002")?.fvalue;
            data.wxshareDescription = configList.FirstOrDefault(x => x.fcode == "003")?.fvalue;
            data.wxLink = _option.WxShareOption.URL + "/index?type=0&cardNum=" + data.fcode;
            data.wximgUrl = _option.WxShareOption.URL + "/common/resource/img/pic1.jpg";
            return data;
        }

        /// <summary>
        /// TODO 根据车牌号获取托运的学校
        /// </summary>
        /// <param name="platenumber"></param>
        /// <returns></returns>
        public async Task<SchoolVD> GetSchoolListByPlatenumber(string platenumber)
        {
            var result = await _schoolBusRepository.GetSchoolListByPlatenumber(platenumber);
            var schoolModes = new List<SchoolMode>();
            result.Select(p => new { p.ftype }).Distinct().ToList().ForEach(x =>
            {
                var schoolMode = new SchoolMode { value = x.ftype.ToString() };
                var typeList = new List<SchoolValueText>();
                result.Where(y => y.ftype == x.ftype).ToList().ForEach(z =>
                {
                    typeList.Add(new SchoolValueText
                    {
                        text = z.text,
                        value = z.value
                    });
                });
                switch (x.ftype)
                {
                    case 1:
                        schoolMode.text = "小学";
                        break;
                    case 2:
                        schoolMode.text = "中学";
                        break;
                    case 3:
                        schoolMode.text = "高中";
                        break;
                }
                schoolMode.children = typeList;
                schoolModes.Add(schoolMode);
            });
            var svd = new SchoolVD()
            {
                status = 1,
                msg = "成功",
                data = schoolModes
            };
            return svd;
        }

    }
}
