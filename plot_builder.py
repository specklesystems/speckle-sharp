# Graph
import plotly
import plotly.express as px
from plotly.subplots import make_subplots
import plotly.graph_objects as go
import math
import colorsys
from itertools import cycle

############################ flowchart detailed
import plotly.graph_objects as go


def plot_flowchart(
    class_send,
    all_sub_groups_sent,
    condition_1,
    all_sub_groups_received,
    condition_2,
    extra_receives: dict[str, dict[str, str]],
    title: str = "Title",
):

    cols = 2
    rows = 1

    specs_col = [
        {"type": "sunburst"},
    ]
    specs = [(specs_col + specs_col) for _ in range(rows)]
    fig = make_subplots(rows=rows, cols=cols, specs=specs)

    groups_send = []
    groups_receive = []
    groups_values = []
    groups_colors = []

    groups_extra_send = []
    groups_extra_mid = []
    groups_extra_receive = []
    groups_extra_values = []
    groups_extra_colors = []

    groups_first_labels = []

    extra_send = list(extra_receives.keys())
    extra_mid = [list(d.keys())[0] for d in list(extra_receives.values())]
    extra_receive = [list(v.values())[0] for v in extra_receives.values()]

    send_set = list(set(all_sub_groups_sent + extra_send))
    mid_set = list(set(extra_mid))
    receive_set = list(set(all_sub_groups_received + extra_receive))

    send_set_indices = []
    for gr_set in send_set:
        found = 0
        for i, gr in enumerate(all_sub_groups_sent):
            if gr == gr_set:
                send_set_indices.append(i)
                found = 1
                break
        if found == 0:
            send_set_indices.append(len(all_sub_groups_sent) + len(send_set_indices))

    receive_set_indices = []
    for gr_set in receive_set:
        found = 0
        for i, gr in enumerate(all_sub_groups_received):
            if gr == gr_set:
                receive_set_indices.append(i)
                found = 1
                break
        if found == 0:
            receive_set_indices.append(
                len(all_sub_groups_received) + len(receive_set_indices)
            )

    send_set = [x for _, x in sorted(zip(send_set_indices, send_set))]
    receive_set = [x for _, x in sorted(zip(receive_set_indices, receive_set))]
    send_set.reverse()
    receive_set.reverse()

    if len(send_set) == 0 or len(receive_set) == 0:
        return

    groups_first_labels = ["__" for _ in send_set]

    for j in range(len(groups_first_labels)):
        # insert empty node
        groups_send.append(j)
        groups_receive.append(len(groups_first_labels) + j)
        groups_values.append(1)
        groups_colors.append("white")

    for i, class_send in enumerate(all_sub_groups_sent):
        class_receive = all_sub_groups_received[i]

        # check if the app matches any of receiving group
        send_index = len(groups_first_labels) + send_set.index(class_send)  # 2nd column
        rec_index = (
            len(groups_first_labels) + len(send_set) + receive_set.index(class_receive)
        )  # 3rd column

        # first, check if already exist:
        exists = 0
        for k, _ in enumerate(groups_send):
            if send_index == groups_send[k] and rec_index == groups_receive[k]:
                # groups_values[k] += 1
                exists += 1
                break
        if exists == 0:
            groups_send.append(send_index)
            groups_receive.append(rec_index)
            groups_values.append(1)
            groups_colors.append(f"rgba(10,132,255,{0.7*condition_1[i]})")

    # extra columns
    for i, class_send in enumerate(extra_send):
        class_mid = extra_mid[i]
        class_receive = extra_receive[i]

        send_index = len(groups_first_labels) + send_set.index(class_send)  # 2nd column
        rec_index = (
            len(groups_first_labels) + len(send_set) + receive_set.index(class_receive)
        )  # 3rd column
        mid_index = (
            len(groups_first_labels)
            + len(send_set)
            + len(receive_set)
            + mid_set.index(class_mid)
        )

        groups_extra_send.append(send_index)
        groups_extra_mid.append(mid_index)
        groups_extra_receive.append(rec_index)
        groups_extra_values.append(1)
        groups_extra_colors.append("rgba(10,132,255,0.7)")

    ##### y-axis
    y_axis12 = [k / (len(send_set) - 0.9999999) + 0.001 for k, _ in enumerate(send_set)]
    y_axis3 = [
        k / (len(receive_set) - 0.9999999) + 0.001 for k, _ in enumerate(receive_set)
    ]
    if y_axis12[-1] > 1:
        y_axis12[-1] = 0.999
    y_axis12.reverse()
    if y_axis3[-1] > 1:
        y_axis3[-1] = 0.999
    y_axis3.reverse()
    y_axis_mid = [
        1 - ((k + 1) * 0.04 / (len(mid_set) + 0.001)) for k, _ in enumerate(mid_set)
    ]
    y_axis = y_axis12 + y_axis12 + y_axis3 + y_axis_mid

    #### x-axis
    x_axis = (
        [0.001 for _ in groups_first_labels]
        + [0.35 for _ in send_set]
        + [0.999 for _ in receive_set]
        + [0.6 for _ in mid_set]
    )

    mynode = dict(
        pad=15,
        thickness=2,
        line=dict(color="black", width=1.5),
        label=groups_first_labels + send_set + receive_set + mid_set,
        x=x_axis,
        y=y_axis,
        color="darkblue",
    )
    mylink = dict(
        source=groups_send
        + groups_extra_send
        + groups_extra_mid,  # indices correspond to labels, eg A1, A2, A1, B1, ...groups_send
        target=groups_receive + groups_extra_mid + groups_extra_receive,
        value=groups_values + groups_extra_values + groups_extra_values,
        color=groups_colors + groups_extra_colors + groups_extra_colors,
    )
    fig1 = go.Figure(data=[go.Sankey(arrangement="snap", node=mynode, link=mylink)])
    fig1.update_layout(title_text="Basic Sankey Diagram", font_size=20)

    fig.add_trace(fig1.data[0], row=1, col=1)

    fig.update_layout(title=title)

    width = 3600
    fig.update_layout(
        autosize=False,
        width=width,
        font_size=20,
        height=int(0.2 * width * max(len(send_set) / 15, 1)),
    )

    fig.update_yaxes(range=[0, 30], col=1)
    fig.update_xaxes(range=[0, 31 * 4], col=1)

    return fig
    # plotly.offline.plot(fig, filename=f"flowchart_{title}.html")


# plot_flowchart(["1", "2", "1", "4", "5"], ["11", "22", "33", "11", "88"])
