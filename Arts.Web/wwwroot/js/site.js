$(function () {
    var antiforgery = $("input[name=__RequestVerificationToken]").val();

    $(".favorite-form").submit(function (e) {
        e.preventDefault();

        var $frm = $(this),
            action = $frm.prop("action"),
            album_id = $frm.find("button").data("content");

        $.post(action, { __RequestVerificationToken: antiforgery, id: album_id }).done(function (result) {
            if (result.success) {
                var $star = $frm.find(".glyphicon-star");
                if (result.favorite) {
                    $star.addClass("active");
                } else {
                    $star.removeClass("active");
                }
            } else {
                window.location.reload();
            }
        });
    });

    $("#MediaFileId").change(changeCover);
    $("#MediaAlbumId").change(changeCover);

    function changeCover() {
        $("img.album-cover").attr("src", $(this).find("option:selected").data("content"));
    }
});
