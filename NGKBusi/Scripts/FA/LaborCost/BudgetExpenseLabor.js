$(document).ready(function () {

    if ($("#tblLCBudgetExpenseLabor").length) {
        $("#tblLCBudgetExpenseLabor").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'output'],
            widgetOptions: {
                stickyHeaders_attachTo: $("#tblLCBudgetExpenseLabor").parent(),
                stickyHeaders_yScroll: $(window),
                output_delivery: 'download',
                output_saveFileName: 'Labor_Cost_BEL.csv',
                output_replaceCR: '&amp;',
                output_replaceTab: 'asfaf',
                filter_saveFilters: true,
                filter_searchDelay: 750
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})'
        });
    }
    $('#btnDownload').on("click", function () {
        $("#tblLCBudgetExpenseLabor").trigger('outputTable');
    });
});