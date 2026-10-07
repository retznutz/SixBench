import{It as e,U as t,W as n,_ as r,d as i,f as a,g as o,k as s,p as c,r as l,tt as u,u as d,z as f,zt as p}from"./DxfkHh_f.js";import{Et as m,mt as h,r as g,t as _,vt as v}from"./Cxvj1CVF.js";import y from"./CVP6RS8G.js";var b=g.extend({name:`dataview`,style:`
    .p-dataview {
        position: relative;
        display: block;
        border-color: dt('dataview.border.color');
        border-width: dt('dataview.border.width');
        border-style: solid;
        border-radius: dt('dataview.border.radius');
        padding: dt('dataview.padding');
    }

    .p-dataview-header {
        background: dt('dataview.header.background');
        color: dt('dataview.header.color');
        border-color: dt('dataview.header.border.color');
        border-width: dt('dataview.header.border.width');
        border-style: solid;
        padding: dt('dataview.header.padding');
        border-radius: dt('dataview.header.border.radius');
    }

    .p-dataview-content {
        background: dt('dataview.content.background');
        border-color: dt('dataview.content.border.color');
        border-width: dt('dataview.content.border.width');
        border-style: solid;
        color: dt('dataview.content.color');
        padding: dt('dataview.content.padding');
        border-radius: dt('dataview.content.border.radius');
    }

    .p-dataview-footer {
        background: dt('dataview.footer.background');
        color: dt('dataview.footer.color');
        border-color: dt('dataview.footer.border.color');
        border-width: dt('dataview.footer.border.width');
        border-style: solid;
        padding: dt('dataview.footer.padding');
        border-radius: dt('dataview.footer.border.radius');
    }

    .p-dataview-paginator-top {
        border-width: dt('dataview.paginator.top.border.width');
        border-color: dt('dataview.paginator.top.border.color');
        border-style: solid;
    }

    .p-dataview-paginator-bottom {
        border-width: dt('dataview.paginator.bottom.border.width');
        border-color: dt('dataview.paginator.bottom.border.color');
        border-style: solid;
    }

    .p-dataview-loading-overlay {
        position: absolute;
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 2;
    }
`,classes:{root:function(e){var t=e.props;return[`p-dataview p-component`,{"p-dataview-list":t.layout===`list`,"p-dataview-grid":t.layout===`grid`}]},header:`p-dataview-header`,pcPaginator:function(e){return`p-dataview-paginator-`+e.position},content:`p-dataview-content`,emptyMessage:`p-dataview-empty-message`,footer:`p-dataview-footer`}}),x={name:`BaseDataView`,extends:_,props:{value:{type:Array,default:null},layout:{type:String,default:`list`},rows:{type:Number,default:0},first:{type:Number,default:0},totalRecords:{type:Number,default:0},paginator:{type:Boolean,default:!1},paginatorPosition:{type:String,default:`bottom`},alwaysShowPaginator:{type:Boolean,default:!0},paginatorTemplate:{type:String,default:`FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink RowsPerPageDropdown`},pageLinkSize:{type:Number,default:5},rowsPerPageOptions:{type:Array,default:null},currentPageReportTemplate:{type:String,default:`({currentPage} of {totalPages})`},sortField:{type:[String,Function],default:null},sortOrder:{type:Number,default:null},lazy:{type:Boolean,default:!1},dataKey:{type:String,default:null}},style:b,provide:function(){return{$pcDataView:this,$parentInstance:this}}};function S(e){return E(e)||T(e)||w(e)||C()}function C(){throw TypeError(`Invalid attempt to spread non-iterable instance.
In order to be iterable, non-array objects must have a [Symbol.iterator]() method.`)}function w(e,t){if(e){if(typeof e==`string`)return D(e,t);var n={}.toString.call(e).slice(8,-1);return n===`Object`&&e.constructor&&(n=e.constructor.name),n===`Map`||n===`Set`?Array.from(e):n===`Arguments`||/^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(n)?D(e,t):void 0}}function T(e){if(typeof Symbol<`u`&&e[Symbol.iterator]!=null||e[`@@iterator`]!=null)return Array.from(e)}function E(e){if(Array.isArray(e))return D(e)}function D(e,t){(t==null||t>e.length)&&(t=e.length);for(var n=0,r=Array(t);n<t;n++)r[n]=e[n];return r}var O={name:`DataView`,extends:x,inheritAttrs:!1,emits:[`update:first`,`update:rows`,`page`],data:function(){return{d_first:this.first,d_rows:this.rows}},watch:{first:function(e){this.d_first=e},rows:function(e){this.d_rows=e},sortField:function(){this.resetPage()},sortOrder:function(){this.resetPage()}},methods:{getKey:function(e,t){return this.dataKey?m(e,this.dataKey):t},onPage:function(e){this.d_first=e.first,this.d_rows=e.rows,this.$emit(`update:first`,this.d_first),this.$emit(`update:rows`,this.d_rows),this.$emit(`page`,e)},sort:function(){var e=this;if(this.value){var t=S(this.value),n=h();return t.sort(function(t,r){var i=m(t,e.sortField),a=m(r,e.sortField);return v(i,a,e.sortOrder,n)}),t}return null},resetPage:function(){this.d_first=0,this.$emit(`update:first`,this.d_first)}},computed:{getTotalRecords:function(){return this.totalRecords?this.totalRecords:this.value?this.value.length:0},empty:function(){return!this.value||this.value.length===0},emptyMessageText:function(){var e;return((e=this.$primevue.config)==null||(e=e.locale)==null?void 0:e.emptyMessage)||``},paginatorTop:function(){return this.paginator&&(this.paginatorPosition!==`bottom`||this.paginatorPosition===`both`)},paginatorBottom:function(){return this.paginator&&(this.paginatorPosition!==`top`||this.paginatorPosition===`both`)},items:function(){if(this.value&&this.value.length){var e=this.value;if(e&&e.length&&this.sortField&&(e=this.sort()),this.paginator){var t=this.lazy?0:this.d_first;return e.slice(t,t+this.d_rows)}return e}return null}},components:{DVPaginator:y}};function k(m,h,g,_,v,y){var b=n(`DVPaginator`);return f(),c(`div`,s({class:m.cx(`root`)},m.ptmi(`root`)),[m.$slots.header?(f(),c(`div`,s({key:0,class:m.cx(`header`)},m.ptm(`header`)),[t(m.$slots,`header`)],16)):a(``,!0),y.paginatorTop?(f(),i(b,{key:1,rows:v.d_rows,first:v.d_first,totalRecords:y.getTotalRecords,pageLinkSize:m.pageLinkSize,template:m.paginatorTemplate,rowsPerPageOptions:m.rowsPerPageOptions,currentPageReportTemplate:m.currentPageReportTemplate,class:e(m.cx(`pcPaginator`,{position:`top`})),alwaysShow:m.alwaysShowPaginator,onPage:h[0]||=function(e){return y.onPage(e)},unstyled:m.unstyled,pt:m.ptm(`pcPaginator`)},o({_:2},[m.$slots.paginatorcontainer?{name:`container`,fn:u(function(e){return[t(m.$slots,`paginatorcontainer`,{first:e.first,last:e.last,rows:e.rows,page:e.page,pageCount:e.pageCount,pageLinks:e.pageLinks,totalRecords:e.totalRecords,firstPageCallback:e.firstPageCallback,lastPageCallback:e.lastPageCallback,prevPageCallback:e.prevPageCallback,nextPageCallback:e.nextPageCallback,rowChangeCallback:e.rowChangeCallback,changePageCallback:e.changePageCallback})]}),key:`0`}:void 0,m.$slots.paginatorstart?{name:`start`,fn:u(function(){return[t(m.$slots,`paginatorstart`)]}),key:`1`}:void 0,m.$slots.paginatorend?{name:`end`,fn:u(function(){return[t(m.$slots,`paginatorend`)]}),key:`2`}:void 0]),1032,[`rows`,`first`,`totalRecords`,`pageLinkSize`,`template`,`rowsPerPageOptions`,`currentPageReportTemplate`,`class`,`alwaysShow`,`unstyled`,`pt`])):a(``,!0),d(`div`,s({class:m.cx(`content`)},m.ptm(`content`)),[y.empty?(f(),c(`div`,s({key:1,class:m.cx(`emptyMessage`)},m.ptm(`emptyMessage`)),[t(m.$slots,`empty`,{layout:m.layout},function(){return[r(p(y.emptyMessageText),1)]})],16)):(f(),c(l,{key:0},[m.$slots.list&&m.layout===`list`?t(m.$slots,`list`,{key:0,items:y.items}):a(``,!0),m.$slots.grid&&m.layout===`grid`?t(m.$slots,`grid`,{key:1,items:y.items}):a(``,!0)],64))],16),y.paginatorBottom?(f(),i(b,{key:2,rows:v.d_rows,first:v.d_first,totalRecords:y.getTotalRecords,pageLinkSize:m.pageLinkSize,template:m.paginatorTemplate,rowsPerPageOptions:m.rowsPerPageOptions,currentPageReportTemplate:m.currentPageReportTemplate,class:e(m.cx(`pcPaginator`,{position:`bottom`})),alwaysShow:m.alwaysShowPaginator,onPage:h[1]||=function(e){return y.onPage(e)},unstyled:m.unstyled,pt:m.ptm(`pcPaginator`)},o({_:2},[m.$slots.paginatorcontainer?{name:`container`,fn:u(function(e){return[t(m.$slots,`paginatorcontainer`,{first:e.first,last:e.last,rows:e.rows,page:e.page,pageCount:e.pageCount,pageLinks:e.pageLinks,totalRecords:e.totalRecords,firstPageCallback:e.firstPageCallback,lastPageCallback:e.lastPageCallback,prevPageCallback:e.prevPageCallback,nextPageCallback:e.nextPageCallback,rowChangeCallback:e.rowChangeCallback,changePageCallback:e.changePageCallback})]}),key:`0`}:void 0,m.$slots.paginatorstart?{name:`start`,fn:u(function(){return[t(m.$slots,`paginatorstart`)]}),key:`1`}:void 0,m.$slots.paginatorend?{name:`end`,fn:u(function(){return[t(m.$slots,`paginatorend`)]}),key:`2`}:void 0]),1032,[`rows`,`first`,`totalRecords`,`pageLinkSize`,`template`,`rowsPerPageOptions`,`currentPageReportTemplate`,`class`,`alwaysShow`,`unstyled`,`pt`])):a(``,!0),m.$slots.footer?(f(),c(`div`,s({key:3,class:m.cx(`footer`)},m.ptm(`footer`)),[t(m.$slots,`footer`)],16)):a(``,!0)],16)}O.render=k;export{O as default};