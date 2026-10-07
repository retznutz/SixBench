import{H as e,It as t,K as n,U as r,W as i,d as a,f as o,k as s,p as c,r as l,u,v as d,z as f,zt as p}from"./DxfkHh_f.js";import{r as m,t as h}from"./Cxvj1CVF.js";import{t as g}from"./C2-R9xpQ.js";var _=m.extend({name:`breadcrumb`,style:`
    .p-breadcrumb {
        background: dt('breadcrumb.background');
        padding: dt('breadcrumb.padding');
        overflow-x: auto;
    }

    .p-breadcrumb-list {
        margin: 0;
        padding: 0;
        list-style-type: none;
        display: flex;
        align-items: center;
        flex-wrap: nowrap;
        gap: dt('breadcrumb.gap');
    }

    .p-breadcrumb-separator {
        display: flex;
        align-items: center;
        color: dt('breadcrumb.separator.color');
    }

    .p-breadcrumb-separator-icon:dir(rtl) {
        transform: rotate(180deg);
    }

    .p-breadcrumb::-webkit-scrollbar {
        display: none;
    }

    .p-breadcrumb-item-link {
        text-decoration: none;
        display: flex;
        align-items: center;
        gap: dt('breadcrumb.item.gap');
        transition:
            background dt('breadcrumb.transition.duration'),
            color dt('breadcrumb.transition.duration'),
            outline-color dt('breadcrumb.transition.duration'),
            box-shadow dt('breadcrumb.transition.duration');
        border-radius: dt('breadcrumb.item.border.radius');
        outline-color: transparent;
        color: dt('breadcrumb.item.color');
    }

    .p-breadcrumb-item-link:focus-visible {
        box-shadow: dt('breadcrumb.item.focus.ring.shadow');
        outline: dt('breadcrumb.item.focus.ring.width') dt('breadcrumb.item.focus.ring.style') dt('breadcrumb.item.focus.ring.color');
        outline-offset: dt('breadcrumb.item.focus.ring.offset');
    }

    .p-breadcrumb-item-link:hover .p-breadcrumb-item-label {
        color: dt('breadcrumb.item.hover.color');
    }

    .p-breadcrumb-item-label {
        transition: inherit;
    }

    .p-breadcrumb-item-icon {
        color: dt('breadcrumb.item.icon.color');
        transition: inherit;
    }

    .p-breadcrumb-item-link:hover .p-breadcrumb-item-icon {
        color: dt('breadcrumb.item.icon.hover.color');
    }
`,classes:{root:`p-breadcrumb p-component`,list:`p-breadcrumb-list`,homeItem:`p-breadcrumb-home-item`,separator:`p-breadcrumb-separator`,separatorIcon:`p-breadcrumb-separator-icon`,item:function(e){return[`p-breadcrumb-item`,{"p-disabled":e.instance.disabled()}]},itemLink:`p-breadcrumb-item-link`,itemIcon:`p-breadcrumb-item-icon`,itemLabel:`p-breadcrumb-item-label`}}),v={name:`BaseBreadcrumb`,extends:h,props:{model:{type:Array,default:null},home:{type:null,default:null}},style:_,provide:function(){return{$pcBreadcrumb:this,$parentInstance:this}}},y={name:`BreadcrumbItem`,hostName:`Breadcrumb`,extends:h,props:{item:null,templates:null,index:null},methods:{onClick:function(e){this.item.command&&this.item.command({originalEvent:e,item:this.item})},visible:function(){return typeof this.item.visible==`function`?this.item.visible():this.item.visible!==!1},disabled:function(){return typeof this.item.disabled==`function`?this.item.disabled():this.item.disabled},label:function(){return typeof this.item.label==`function`?this.item.label():this.item.label},isCurrentUrl:function(){var e=this.item,t=e.to,n=e.url,r=typeof window<`u`?window.location.pathname:``;return t===r||n===r?`page`:void 0}},computed:{ptmOptions:function(){return{context:{item:this.item,index:this.index}}},getMenuItemProps:function(){var e=this;return{action:s({class:this.cx(`itemLink`),"aria-current":this.isCurrentUrl(),onClick:function(t){return e.onClick(t)}},this.ptm(`itemLink`,this.ptmOptions)),icon:s({class:[this.cx(`icon`),this.item.icon]},this.ptm(`icon`,this.ptmOptions)),label:s({class:this.cx(`label`)},this.ptm(`label`,this.ptmOptions))}}}},b=[`href`,`target`,`aria-current`];function x(e,r,i,l,u,d){return d.visible()?(f(),c(`li`,s({key:0,class:[e.cx(`item`),i.item.class]},e.ptm(`item`,d.ptmOptions)),[i.templates.item?(f(),a(n(i.templates.item),{key:1,item:i.item,label:d.label(),props:d.getMenuItemProps},null,8,[`item`,`label`,`props`])):(f(),c(`a`,s({key:0,href:i.item.url||`#`,class:e.cx(`itemLink`),target:i.item.target,"aria-current":d.isCurrentUrl(),onClick:r[0]||=function(){return d.onClick&&d.onClick.apply(d,arguments)}},e.ptm(`itemLink`,d.ptmOptions)),[i.templates&&i.templates.itemicon?(f(),a(n(i.templates.itemicon),{key:0,item:i.item,class:t(e.cx(`itemIcon`,d.ptmOptions))},null,8,[`item`,`class`])):i.item.icon?(f(),c(`span`,s({key:1,class:[e.cx(`itemIcon`),i.item.icon]},e.ptm(`itemIcon`,d.ptmOptions)),null,16)):o(``,!0),i.item.label?(f(),c(`span`,s({key:2,class:e.cx(`itemLabel`)},e.ptm(`itemLabel`,d.ptmOptions)),p(d.label()),17)):o(``,!0)],16,b))],16)):o(``,!0)}y.render=x;var S={name:`Breadcrumb`,extends:v,inheritAttrs:!1,components:{BreadcrumbItem:y,ChevronRightIcon:g}};function C(t,n,p,m,h,g){var _=i(`BreadcrumbItem`),v=i(`ChevronRightIcon`);return f(),c(`nav`,s({class:t.cx(`root`)},t.ptmi(`root`)),[u(`ol`,s({class:t.cx(`list`)},t.ptm(`list`)),[t.home?(f(),a(_,s({key:0,item:t.home,class:t.cx(`homeItem`),templates:t.$slots,pt:t.pt,unstyled:t.unstyled},t.ptm(`homeItem`)),null,16,[`item`,`class`,`templates`,`pt`,`unstyled`])):o(``,!0),(f(!0),c(l,null,e(t.model,function(e,n){return f(),c(l,{key:e.label+`_`+n},[t.home||n!==0?(f(),c(`li`,s({key:0,class:t.cx(`separator`)},{ref_for:!0},t.ptm(`separator`)),[r(t.$slots,`separator`,{},function(){return[d(v,s({"aria-hidden":`true`,class:t.cx(`separatorIcon`)},{ref_for:!0},t.ptm(`separatorIcon`)),null,16,[`class`])]})],16)):o(``,!0),d(_,{item:e,index:n,templates:t.$slots,pt:t.pt,unstyled:t.unstyled},null,8,[`item`,`index`,`templates`,`pt`,`unstyled`])],64)}),128))],16)],16)}S.render=C;export{S as default};