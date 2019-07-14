﻿$(function () {
    // 与学生关系选择
    $("#relationshipname").click(function () {
        var picker = new mui.PopPicker();
        picker.setData([{
            value: '父亲',
            text: '父亲'
        }, {
            value: '母亲',
            text: '母亲'
        }, {
            value: '爷爷',
            text: '爷爷'
        }, {
            value: '奶奶',
            text: '奶奶'
        }, {
            value: '姥爷',
            text: '姥爷'
        }, {
            value: '姥姥',
            text: '姥姥'
        }, {
            value: '其他',
            text: '其他'
        }]);
        picker.show(function (selectItems) {
            $("#relationship").val(selectItems[0].text);
        })
    });
});

// 倒计时
var countdown = 60;
var _generate_code = $("#generateCode");
function settime() {
    if (countdown === 0) {
        _generate_code.attr("disabled", false);
        _generate_code.text("获取验证码");
        countdown = 60;
        return false;
    } else {
        _generate_code.attr("disabled", true);
        _generate_code.text("重新发送(" + countdown + ")");
        countdown--;
    }
    setTimeout(function () {
        settime();
    }, 1000);
}

// 发短信
function sendSmsCode(codetype) {
    var phone = $("#phoneNum").val();
    if (phone != "") {
        settime();
        getAjax("/SchoolBus/SendSmsCode", { phoneNum: phone, verificationCodeType: codetype }, function (data) {
            if (data.status != 1) {
                alert(data.msg);
            }
        }, function () {});
    } else {
        alert("请输入手机号");
    }
}

// 通用ajax+防止csrf
function getAjax(url, parm, callBack, cmBack) {
    var form = $("#frm");
    var token = $('input[name="__RequestVerificationToken"]', form).val();
    parm.__RequestVerificationToken = token;
    $.ajax({
        type: 'post',
        url: url,
        data: parm,
        //async: false,
        success: function (msg) {
            callBack(msg);
        },
        error: function (result) {
        },
        complete: function (XMLHttpRequest, textStatus) {
            cmBack();
        }
    });
}

(function () {
    //先判断是否为微信浏览器
    var ua = window.navigator.userAgent.toLowerCase();
    if (ua.match(/MicroMessenger/i) == 'micromessenger') {
        //微信浏览器中，aler弹框不显示域名
        //重写alert方法，alert()方法重写，不能传多余参数
        window.alert = function (name) {
            var iframe = document.createElement("IFRAME");
            iframe.style.display = "none";
            iframe.setAttribute("src", 'data:text/plain');
            document.documentElement.appendChild(iframe);
            window.frames[0].window.alert(name);
            iframe.parentNode.removeChild(iframe);
        }
    }
    //微信浏览器中，confirm弹框不显示域名
    //if (ua.match(/MicroMessenger/i) == 'micromessenger') {
    //    //重写confirm方法，confirm()方法重写，不能传多余参数
    //    window.confirm = function (message) {
    //        var iframe = document.createElement("IFRAME");
    //        iframe.style.display = "none";
    //        iframe.setAttribute("src", 'data:text/plain,');
    //        document.documentElement.appendChild(iframe);
    //        var alertFrame = window.frames[0];
    //        var result = alertFrame.window.confirm(message);
    //        iframe.parentNode.removeChild(iframe);
    //        return result;
    //    };
    //}
})();
//(function () {
//    // 监听返回按钮
//    pushHistory();
//    window.addEventListener("popstate", function (e) {
//        //关闭当前浏览器
//        wx.closeWindow();
//    }, false);
//    function pushHistory() {
//        var state = {
//            title: "title",
//            url: "#"
//        };
//        window.history.pushState(state, "title", "#");
//    }
//})();

