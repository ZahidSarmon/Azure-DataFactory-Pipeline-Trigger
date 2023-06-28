Vue.use(VTooltip);

$(document).ready(function () {
    $('.setLanguage').click(function () {
        var val = $(this).data('val');

        helper.post('/Culture/SetLanguage?lan=' + val, {}, function (res) {
            location.reload();
        }, true, false, false, false);
    });
});


let layoutNavBarApp = new Vue({
    el: "#vc_layout_navbar",
    data: {
        user: {
            displayName: '',
        }
    },
    mounted: function () {
    },
    methods: {
        signOut: function () {
            helper.post("/Login/SignOut",
                JSON.stringify({}),
                (response) => {
                    if (response.success) {

                        $.notify(response.message, "success");

                        location.href = window.location.protocol + "//" + window.location.hostname + (window.location.port ? ":" + window.location.port : "") + "/Login";
                    } else {
                        $.notify(response.message, "error");
                    }
                });
        }
    }
});