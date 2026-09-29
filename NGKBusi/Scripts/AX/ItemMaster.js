$(document).ready(function () {
    if ($("#tbAXItemMaster").length) {
        $("#tbAXItemMaster").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'QCCUserList.csv',
                output_replaceCR: '&amp;'
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: 50
        });
    }

    $("#selAXItemGroup").change(function () {
        var currID = $(this).val();
        $("#selAXItemType").closest(".form-group").LoadingOverlay("show");
        $.ajax({
            url: "ItemMaster/getClassSub",
            method: "POST",
            data: {
                iID : currID
            }, success: function (data) {
                $("#selAXItemType option[value!='']").remove();
                $.each(data, function (e, v) {
                    $("#selAXItemType").append(new Option(v.Description, v.ID));
                });
                $("#selAXItemType").closest(".form-group").LoadingOverlay("hide",true);
            }
        });
    });
});