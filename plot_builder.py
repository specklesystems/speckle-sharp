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


def plot_flowchart(class_send, all_sub_groups_sent, condition_1, all_sub_groups_received, condition_2, title: str = "Title"):

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

    groups_send_labels = []
    groups_receive_labels = []
    groups_first_labels = []

    send_set = list(set(all_sub_groups_sent))
    receive_set = list(set(all_sub_groups_received))
    send_set.sort()
    send_set.reverse()
    receive_set.sort()
    receive_set.reverse()

    if len(send_set) == 0 or len(receive_set) == 0:
        return

    groups_send_labels = [gr for gr in send_set]
    groups_receive_labels = [gr for gr in receive_set]
    groups_first_labels = ["__" for _ in send_set]

    # for k, _ in enumerate([1]):
    any_connection_in_subgroup = 0

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
        rec_index = 2 * len(groups_first_labels) + receive_set.index(
            class_receive
        )  # 3rd column

        # first, check if already exist:
        exists = 0
        for k, _ in enumerate(groups_send):
            if send_index == groups_send[k] and rec_index == groups_receive[k]:
                groups_values[k] += 1
                exists += 1
                break
        if exists == 0:
            groups_send.append(send_index)
            groups_receive.append(rec_index)
            groups_values.append(1)
            groups_colors.append("x")

            any_connection_in_subgroup += 1

    if any_connection_in_subgroup == 0:
        for n, _ in enumerate(groups_receive_labels):
            groups_send.append(len(groups_first_labels) + n)
            groups_receive.append(2 * len(groups_first_labels) + n)
            groups_values.append(1)
            groups_colors.append("white")

    #########################################

    groups_indices = [
        0.01 * (j + 1) if c == "white" else j + 1 for j, c in enumerate(groups_colors)
    ]

    groups_send = [x for _, x in sorted(zip(groups_indices, groups_send))]
    groups_receive = [x for _, x in sorted(zip(groups_indices, groups_receive))]
    groups_values = [x for _, x in sorted(zip(groups_indices, groups_values))]
    groups_colors = [x for _, x in sorted(zip(groups_indices, groups_colors))]
    color_scale = px.colors.sequential.thermal[:]

    for k, c in enumerate(groups_colors):
        if c != "white":
            index = int((groups_values[k] / max(groups_values)) * len(color_scale))
            if index == len(color_scale):
                index -= 1
            groups_colors[k] = color_scale[index]

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
    y_axis = y_axis12 + y_axis12 + y_axis3

    #### x-axis
    x_axis = (
        [0.001 for _ in groups_first_labels]
        + [0.35 for _ in groups_send_labels]
        + [0.999 for _ in groups_receive_labels]
    )

    mynode = dict(
        pad=15,
        thickness=2,
        line=dict(color="black", width=0.5),
        label=groups_first_labels + groups_send_labels + groups_receive_labels + [""],
        x=x_axis,
        y=y_axis,
        color="darkblue",
    )
    mylink = dict(
        source=groups_send,  # indices correspond to labels, eg A1, A2, A1, B1, ...groups_send
        target=groups_receive,
        value=groups_values,
        color=groups_colors,
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
        height=int(0.2 * width * max(len(groups_send_labels) / 15, 1)),
    )

    fig.update_yaxes(range=[0, 30], col=1)
    fig.update_xaxes(range=[0, 31 * 4], col=1)

    return(fig)
    #plotly.offline.plot(fig, filename=f"flowchart_{title}.html")


# plot_flowchart(["1", "2", "1", "4", "5"], ["11", "22", "33", "11", "88"])
