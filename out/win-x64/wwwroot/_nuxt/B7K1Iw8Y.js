import{U as e,W as t,k as n,p as r,v as i,z as a}from"./DxfkHh_f.js";import{r as o}from"./Cxvj1CVF.js";import{n as s}from"./TEJK6YQH.js";var c=o.extend({name:`overlaybadge`,style:`
    .p-overlaybadge {
        position: relative;
    }

    .p-overlaybadge .p-badge {
        position: absolute;
        inset-block-start: 0;
        inset-inline-end: 0;
        transform: translate(50%, -50%);
        transform-origin: 100% 0;
        margin: 0;
        outline-width: dt('overlaybadge.outline.width');
        outline-style: solid;
        outline-color: dt('overlaybadge.outline.color');
    }

    .p-overlaybadge .p-badge:dir(rtl) {
        transform: translate(-50%, -50%);
    }
`,classes:{root:`p-overlaybadge`}}),l={name:`OverlayBadge`,extends:{name:`OverlayBadge`,extends:s,style:c,provide:function(){return{$pcOverlayBadge:this,$parentInstance:this}}},inheritAttrs:!1,components:{Badge:s}};function u(o,s,c,l,u,d){var f=t(`Badge`);return a(),r(`div`,n({class:o.cx(`root`)},o.ptmi(`root`)),[e(o.$slots,`default`),i(f,n(o.$props,{pt:o.ptm(`pcBadge`)}),null,16,[`pt`])],16)}l.render=u;export{l as default};