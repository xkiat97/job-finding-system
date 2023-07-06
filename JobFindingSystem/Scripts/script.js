
// TODO: Navigate to the URL given
$("[data-url]").click(function (e) {
    e.preventDefault();
    var url = $(this).data("url");
    location = url;
});
